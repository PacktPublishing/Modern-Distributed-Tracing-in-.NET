using System.Collections.Concurrent;
using System.Diagnostics;

namespace links;

internal class Producer
{
    private static ActivitySource Source = new(nameof(Producer));
    private readonly ConcurrentQueue<WorkItem> _queue;
    public Producer(ConcurrentQueue<WorkItem> queue)
    {
        _queue = queue;
    }

    public void Enqueue(int id)
    {
        using var enqueue = Source.StartActivity(ActivityKind.Producer)?
            .SetTag("work_item.id", id);
        
        _queue.Enqueue(new WorkItem(id, enqueue?.Context));
    }
}
