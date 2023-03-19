using Azure.Storage.Queues;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace consumer;

public class QueueSizeReporter : BackgroundService
{
    private static readonly TimeSpan CollectionInterval = TimeSpan.FromSeconds(5);
    private readonly Meter _meter = new ("Queue.Size");
    private readonly ObservableGauge<long> _queueSize;
    private readonly QueueClient _queue;
    private readonly ILogger<QueueSizeReporter> _logger;
    private int _currentQueueSize;

    public QueueSizeReporter(QueueClient queue, ILogger<QueueSizeReporter> logger)
    {
        _queue = queue;
        TagList tags = new ()
        {
            { "net.peer.name", queue.AccountName},
            { "messaging.source.name", queue.Name}
        };
        _queueSize = _meter.CreateObservableGauge(
            "messaging.azqueues.queue.size",
            () => new Measurement<long>(_currentQueueSize, tags),
            "messages",
            "Approximate number of messages in the queue.");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_queueSize.Enabled)
            {
                try
                {
                    var res = await _queue.GetPropertiesAsync(token);
                    _currentQueueSize = res.Value.ApproximateMessagesCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "can't retrieve queue size");
                }
            }
            await Task.Delay(CollectionInterval, token);
        }
    }

    public override void Dispose()
    {
        _meter.Dispose();
        base.Dispose();
    }
}
