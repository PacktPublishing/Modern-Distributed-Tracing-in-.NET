using System.Diagnostics;

namespace metrics;

public readonly struct WorkItem
{
    public WorkItem(long seqNo) : this(seqNo, Activity.Current?.Context)
    {
    }

    public WorkItem(long seqNo, ActivityContext? context)
    {
        SequenceNumber = seqNo;
        Context = context ?? default;
        CreatedTimeUtc = DateTime.UtcNow;
    }

    public ActivityContext Context { get;}

    public DateTimeOffset CreatedTimeUtc { get; }
    public long SequenceNumber { get; }
}
