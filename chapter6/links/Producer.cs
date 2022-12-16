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

    public void Enqueue(int id, string command)
    {
        using var activity = Source.StartActivity(ActivityKind.Producer);
        if (activity?.IsAllDataRequested == true)
        {
            activity.SetTag("work_item.id", id);
            activity.SetTag("work_item.command", command);
        }
        
        _queue.Enqueue(new WorkItem(id, command, activity?.Context ?? default));
    }
}
