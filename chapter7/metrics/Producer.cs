using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace metrics;

internal class Producer
{
    private static readonly ActivitySource Source = new(nameof(Producer));
    private static readonly Meter Meter = new("queue.producer");
    private static readonly Counter<long> EnqueuedCounter = Meter.CreateCounter<long>("queue.enqueued.count", "{count}", "Number of enqueued work items");
    private readonly ConcurrentQueue<WorkItem> _queue;
    private readonly string _queueName;

    public Producer(ConcurrentQueue<WorkItem> queue, string queueName)
    {
        _queue = queue;
        _queueName = queueName;
    }

    public void Enqueue(long sequenceNumber)
    {
        using var enqueue = Source.StartActivity(ActivityKind.Producer)?
            .SetTag("work_item.sequence_number", sequenceNumber)
            .SetTag("work_item.queue_name", _queueName);

        _queue.Enqueue(new WorkItem(sequenceNumber));
        EnqueuedCounter.Add(1, new KeyValuePair<string, object?>("queue", _queueName));
    }
}
