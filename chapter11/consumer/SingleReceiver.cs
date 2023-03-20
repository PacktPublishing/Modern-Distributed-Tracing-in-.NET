using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace consumer;

public class SingleReceiver : BackgroundService
{
    private static readonly ThreadLocal<Random> Random = new (() => new Random());
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BackoffTimeout = TimeSpan.FromSeconds(1);
    private readonly ActivitySource _messageSource = new("Queue.Message");
    private readonly ActivitySource _receiverSource = new("Queue.Receive");
    private readonly Meter _meter = new ("Queue.Receive");
    private readonly Histogram<double> _consumerLag;
    private readonly Histogram<double> _loopDuration;
    private readonly ILogger<BatchReceiver> _logger;
    private readonly QueueClient _queue;
    private readonly TextMapPropagator _propagator;

    public SingleReceiver(QueueClient queueClient, TextMapPropagator propagator, ILogger<BatchReceiver> logger)
    {
        _queue = queueClient;
        _logger = logger;
        _propagator = propagator;
        _consumerLag = _meter.CreateHistogram<double>("messaging.azqueues.consumer.lag", "s", "Approximate lag between the time message was send and received.");
        _loopDuration = _meter.CreateHistogram<double>("messaging.azqueues.process.loop.duration", "ms", "Receive and processing duration.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Stopwatch? duration = Stopwatch.StartNew(); // we can check if metrics are enabled first
            var token = new CancellationTokenSource(ProcessingTimeout).Token;
            using var act = _receiverSource.StartActivity("ReceiveAndProcess", ActivityKind.Server);
            try
            {
                var response = await _queue.ReceiveMessagesAsync(1, ProcessingTimeout, token);
                QueueMessage[] messages = response.Value;
                RecordLag(messages);
                if (messages.Length == 0)
                {
                    await Task.Delay(1000, token);
                    act?.SetStatus(ActivityStatusCode.Unset, "empty");
                    RecordLoopDuration(duration, "empty");
                    continue;
                }

                act?.SetTag("messaging.message.id", messages[0].MessageId);
                await ProcessAndSettle(messages[0], token);
                RecordLoopDuration(duration, "ok");
            }
            catch (Exception ex)
            {
                act?.SetStatus(ActivityStatusCode.Error, ex.Message);
                RecordLoopDuration(duration, "fail");
                _logger.LogError(ex, "processing loop failed");
            } 
        }
    }

    private async Task ProcessAndSettle(QueueMessage msg, CancellationToken token)
    {
        using var act = StartProcessActivity(msg);
        _logger.LogInformation("processing message {id} {text}", msg.MessageId, msg.Body);

        try
        {
            await ProcessMessage(msg, token);
            await _queue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, token);
        }
        catch (Exception ex)
        {
            await _queue.UpdateMessageAsync(msg.MessageId, msg.PopReceipt, visibilityTimeout: BackoffTimeout, cancellationToken: token);
            _logger.LogError(ex, "Processing failed");
            act?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    // the rest is the same between batch and single
    private async Task ProcessMessage(QueueMessage message, CancellationToken cancellationToken)
    {
        Message msg = message.Body.ToObjectFromJson<Message>();
        _logger.LogInformation("received message {message}", msg.Text);

        await Task.Delay(Random.Value!.Next(500), cancellationToken);
    }

    private Activity? StartProcessActivity(QueueMessage msg)
    {
        PropagationContext ctx = ExtractContext(msg);
        // keep the current activity as link since we're going to use message as parent
        var current = new ActivityLink(Activity.Current?.Context ?? default);
        var act = _messageSource.StartActivity($"{_queue.Name} process", ActivityKind.Consumer, ctx.ActivityContext, links: new[] { current });
        if (act?.IsAllDataRequested == true)
        {
            act.SetTag("messaging.system", "azqueues")
               .SetTag("messaging.operation", "process")
               .SetTag("messaging.source.name", _queue.Name)
               .SetTag("net.peer.name", _queue.AccountName)
               .SetTag("messaging.message.id", msg.MessageId)
               .SetTag("messaging.azqueues.message.dequeue_count", msg.DequeueCount)
               .SetTag("messaging.azqueues.message.inserted_on", msg.InsertedOn)
               .SetTag("messaging.azqueues.message.next_visible_on", msg.NextVisibleOn);
        }

        return act;
    }

    private void RecordLag(QueueMessage[] messages)
    {
        if (!_consumerLag.Enabled || messages.Length == 0) return;

        long receivedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        TagList tags = new ()
        {
            { "net.peer.name", _queue.AccountName },
            { "messaging.source.name", _queue.Name }
        };
        

        foreach (var msg in messages.Where(m => m.InsertedOn.HasValue))
        {
            long insertedOn = msg.InsertedOn!.Value.ToUnixTimeMilliseconds();
            long lag = Math.Max(1, receivedAt - insertedOn);
            _consumerLag.Record(lag / 1000d, tags);
        }
    }

    private void RecordLoopDuration(Stopwatch duration, string status)
    {
        TagList tags = new ()
        {
            { "net.peer.name", _queue.AccountName },
            { "messaging.source.name", _queue.Name },
            { "messaging.azqueue.status", status }
        };
        _loopDuration.Record(duration.ElapsedMilliseconds, tags);
    }

    private PropagationContext ExtractContext(QueueMessage message)
    {
        var payload = message.Body.ToObjectFromJson<Message>();
        return _propagator.Extract(default, payload, ExtractValue);
    }

    private IEnumerable<string> ExtractValue(Message message, string key)
    {
        if (message.Headers.TryGetValue(key, out var value) && value is string valueStr)
        {
            return new[] { valueStr };
        }

        return Enumerable.Empty<string>();
    }

    public override void Dispose()
    {
        _messageSource.Dispose();
        _receiverSource.Dispose();
        _meter.Dispose();
        base.Dispose();
    }
}
