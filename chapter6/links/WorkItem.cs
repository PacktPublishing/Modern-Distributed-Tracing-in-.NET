using System.Diagnostics;

namespace links;

public readonly struct WorkItem
{
    public WorkItem(int id, ActivityContext? context)
    {
        Id = id;
        Context = context ?? default;
        CreatedTimeUtc = DateTime.UtcNow;
    }

    public int Id { get;}

    public ActivityContext Context { get;}

    public DateTimeOffset CreatedTimeUtc { get; }
}
