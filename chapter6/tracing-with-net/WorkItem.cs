namespace tracing_with_net;

public readonly struct WorkItem
{
    public WorkItem(int id, string command)
    {
        Id = id;
        Command = command;
    }

    public int Id { get;}
    public string Command { get;}
}
