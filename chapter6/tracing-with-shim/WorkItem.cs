namespace tracing_with_otel_api;

public readonly struct WorkItem
{
    public WorkItem(int id, string command)
    {
        Id = id;
        Command = command;
    }

    public int Id { get; }
    public string Command { get; }
}
