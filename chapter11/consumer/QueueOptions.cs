public class QueueOptions
{
    public string? ConnectionString { get; set; }
    public string? Name { get; set; }
    public int MaxBatchSize { get; set; } = 1;
}