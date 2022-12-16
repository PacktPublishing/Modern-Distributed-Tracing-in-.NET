using OpenTelemetry.Trace;
using System.Diagnostics;

namespace tracing_with_otel_api;

internal class Worker
{
    private static readonly Tracer Tracer = TracerProvider.Default.GetTracer("Worker");

    public static async Task DoWork(WorkItem work)
    {
        using TelemetrySpan workSpan = Tracer
            .StartActiveSpan(nameof(DoWork));
        workSpan.SetAttribute("work_item.id", work.Id);
        workSpan.SetAttribute("work_item.command", work.Command);

        await DoWithRetry(async tryNumber =>
        {
            using var trySpan = Tracer.StartSpan("Try");
            try
            {
                await DoWorkImpl(work.Id, tryNumber);
                trySpan.SetAttribute("try_count", tryNumber);
            }
            catch (Exception ex)
            {
                trySpan.RecordException(ex);
                trySpan.SetStatus(Status.Error);
                throw;
            }
        });
    }

    private static async Task DoWithRetry(Func<int, Task> action)
    {
        const int maxTries = 3;
        List<Exception>? exceptions = null;

        for (int t = 0; t < maxTries; t++)
        {
            try
            {
                await action(t);
                return;
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Tracer.CurrentSpan.SetStatus(Status.Error);

        throw new AggregateException(exceptions!);
    }

    private static async Task DoWorkImpl(int workItemId, int tryNumber)
    {
        Console.WriteLine($"Doing work with id = '{workItemId}'");
        if (tryNumber % 2 == 0)
        {
            throw new Exception("something went wrong");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }
}
