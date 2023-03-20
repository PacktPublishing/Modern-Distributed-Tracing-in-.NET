using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace producer.Controllers;

[ApiController]
[Route("[controller]")]
public class SendController : ControllerBase
{
    private static readonly ActivitySource Source = new ("Queue.Publish");
    private static readonly Meter Meter = new("Queue.Publish");
    private static readonly Histogram<double> PublishDuration = Meter.CreateHistogram<double>("messaging.azqueues.publish.duration", "ms", "Publish call duration.");
    private readonly ILogger<SendController> _logger;
    private readonly QueueClient _queue;
    private readonly TextMapPropagator _propagator;

    public SendController(QueueClient queueClient, TextMapPropagator propagator, ILogger<SendController> logger)
    {
        _queue = queueClient;
        _propagator = propagator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<SendReceipt> SendMessage([FromQuery]bool malformed)
    {
        var message = malformed ? new Message(string.Empty) : new Message(new { hello = "tracing" });
        return await SendInstrumented(message);
    }

    [HttpGet("{count}")]
    public Task<SendReceipt[]> SendMessages(int count)
    {
        var sendTasks = Enumerable.Range(0, count)
                    .Select(i => 
                    {
                        var payload = new { hello = $"tracing {i}" };
                        var message = new Message(payload);
                        message.Headers.Add("index", i.ToString());
                        return SendInstrumented(message);
                    });

        return Task.WhenAll(sendTasks);
    }

    private async Task<SendReceipt> SendInstrumented(Message message)
    {
        Stopwatch? duration = PublishDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartPublishActivity();
        // let's inject current in case per-message activities are not enabled
        InjectContext(message, Activity.Current);
        try
        {
            var receipt = await _queue.SendMessageAsync(BinaryData.FromObjectAsJson(message));

            act?.SetTag("messaging.message.id", receipt.Value.MessageId);
            RecordPublishMetrics(duration, "ok");
            return receipt;
        }
        catch (Exception ex)
        {
            act?.SetStatus(ActivityStatusCode.Error, ex.Message);
            RecordPublishMetrics(duration, "fail");
            _logger.LogError(ex, "send failed");
            throw;
        }
    }

    private Activity? StartPublishActivity()
    {
        var act = Source.StartActivity($"{_queue.Name} publish", ActivityKind.Producer);

        if (act?.IsAllDataRequested == true)
            act.SetTag("messaging.system", "azqueues")
               .SetTag("messaging.operation", "publish")
               .SetTag("messaging.destination.name", _queue.Name)
               .SetTag("net.peer.name", _queue.AccountName);

        return act;
    }

    public void RecordPublishMetrics(Stopwatch? dur, string status)
    {
        if (dur == null)
        {
            return;
        }

        TagList tags = new()
        {
            { "net.peer.name", _queue.AccountName },
            { "messaging.destination.name", _queue.Name },
            { "messaging.azqueue.status", status }
        };
        PublishDuration.Record(dur.Elapsed.TotalMilliseconds, tags);
    }

    private void InjectContext(Message message, Activity? act)
    {
        if (act != null)
        {
            PropagationContext context = new(act.Context, Baggage.Current);
            _propagator.Inject(context, message,
                static (m, k, v) => m.Headers[k] = v);
        }
    }
}
