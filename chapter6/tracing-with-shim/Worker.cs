using OpenTelemetry.Trace;

namespace tracing_with_otel_api;

internal class Worker
{
    private static readonly Tracer Tracer = TracerProvider.Default.GetTracer("Worker");

    public static async Task DoWork(int workItemId)
    {
        using var workSpan = Tracer.StartActiveSpan("DoWork");
        workSpan.SetAttribute("work_item.id", workItemId);
        try
        {
            await DoWorkImpl(workItemId);
        }
        catch (Exception ex)
        {
            workSpan.SetStatus(Status.Error.WithDescription(ex.Message));
            throw;
        }
    }

    private static async Task DoWorkImpl(int workItemId)
    {
        Console.WriteLine($"Doing work with id = '{workItemId}'");
        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }
}
