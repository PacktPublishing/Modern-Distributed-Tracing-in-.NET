using System.Collections.Concurrent;
using System.Diagnostics;

namespace links;

internal class Processor
{
    private static readonly ActivitySource Source = new(nameof(Processor));
    private readonly ConcurrentQueue<WorkItem> _queue;
    
    private readonly CancellationTokenSource _cts = new ();
    private Task? _loop = null;

    public Processor(ConcurrentQueue<WorkItem> queue)
    {
        _queue = queue;
    }

    public void Start()
    {
        _loop = Task.Run(async () =>
        {
            while (!(_cts.IsCancellationRequested && _queue.IsEmpty))
            {
                if (_queue.TryDequeue(out var item))
                {
                    await Process(item);
                }
                else if (_queue.IsEmpty)
                {
                    await Task.Delay(100);
                }
            }
        });
    }

    private static async Task Process(WorkItem item)
    {
        using var activity = Source.StartActivity(ActivityKind.Consumer, item.Context);
        if (activity?.IsAllDataRequested == true)
        {
            TimeSpan timeInQueue = DateTimeOffset.UtcNow - item.CreatedTimeUtc;
            activity.SetTag("work_item.time_in_queue_ms", timeInQueue.TotalMilliseconds);
            activity.SetTag("work_item.id", item.Id);
        }

        await Task.Delay(10);
        Console.WriteLine($"Processed work item {item.Id}");
    }

    public Task Stop()
    {
        _cts.Cancel();
        return _loop ?? Task.CompletedTask;
    }
}
