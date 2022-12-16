using System.Collections.Concurrent;
using System.Diagnostics;

namespace links;

internal class BatchProcessor
{
    private const int BatchSize = 3;
    private static readonly ActivitySource Source = new(nameof(Processor));
    private readonly ConcurrentQueue<WorkItem> _queue;
    
    private readonly CancellationTokenSource _cts = new ();
    private Task? _loop = null;

    public BatchProcessor(ConcurrentQueue<WorkItem> queue)
    {
        _queue = queue;
    }

    public void Start()
    {
        _loop = Task.Run(async () =>
        {
            while (!(_cts.IsCancellationRequested && _queue.IsEmpty))
            {
                Stopwatch start = Stopwatch.StartNew();
                var batch = new List<WorkItem>();
                while (batch.Count < BatchSize && start.Elapsed < TimeSpan.FromSeconds(1))
                {
                    if (_queue.TryDequeue(out var item))
                    {
                        batch.Add(item);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }

                if (batch.Count > 0)
                {
                    await ProcessBatch(batch);
                }
            }
        });
    }

    private static async Task ProcessBatch(List<WorkItem> items)
    {
        using var activity = Source.StartActivity(ActivityKind.Consumer, 
            links: items.Select(i => new ActivityLink(i.Context)));

        if (activity?.IsAllDataRequested == true)
        {
            activity.SetTag("work_items.id", items.Select(i => i.Id).ToArray());
            activity.SetTag("work_items.command", items.Select(i => i.Command).ToArray());
            activity.SetTag("work_items.time_in_queue_ms", items.Select(i => GetTimeInQueue(i.CreatedTimeUtc)).ToArray());
        }


        // process work items
        await Task.Delay(10);

        Console.WriteLine($"Processed work items {string.Join(", ", items.Select(i => i.Id))}");
    }

    private static double GetTimeInQueue(DateTimeOffset createdTimeUtc) => 
        (DateTimeOffset.UtcNow - createdTimeUtc).TotalMilliseconds;

    public Task Stop()
    {
        _cts.Cancel();
        return _loop ?? Task.CompletedTask;
    }
}
