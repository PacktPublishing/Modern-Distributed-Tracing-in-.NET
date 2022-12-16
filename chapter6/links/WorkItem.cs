using System.Diagnostics;

namespace links;

public readonly struct WorkItem
{
    public WorkItem(int id, string command, ActivityContext context)
    {
        Id = id;
        Command = command;
        Context = context;
        CreatedTimeUtc = DateTime.UtcNow;
    }

    public int Id { get;}
    public string Command { get;}

    public ActivityContext Context { get;}

    public DateTimeOffset CreatedTimeUtc { get; }
}
