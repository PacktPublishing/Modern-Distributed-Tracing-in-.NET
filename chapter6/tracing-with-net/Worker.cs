using OpenTelemetry.Trace;
using System.Diagnostics;

namespace tracing_with_net;

internal class Worker
{
    private static readonly ActivitySource Source = new ("Worker");

    public static async Task DoWork(WorkItem work)
    {
        using var workActivity = Source.StartActivity();
        workActivity?.AddTag("work_item.id", work.Id);
        workActivity?.AddTag("work_item.command", work.Command);

        await DoWithRetry(async tryNumber =>
        {
            using var tryActivity = Source.StartActivity("Try");
            try
            {
                await DoWorkImpl(work.Id, tryNumber);
                tryActivity?.SetTag("try_count", tryNumber);
            }
            catch (Exception ex)
            {
                tryActivity?.RecordException(ex);
                tryActivity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        });
    }
  
    public static async Task DoWorkWithBasicTracing(int workItemId)
    {
        using var activity = Source.StartActivity("DoWork");
        activity?.SetTag("work_item.id", workItemId);
        try
        {
            await DoWorkImpl(workItemId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task DoWorkImpl(int workItemId)
    {
        Console.WriteLine($"Processing work item {workItemId}");
        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }

    private static Task DoWorkImpl(int workItemId, int tryNumber)
    {
        if (tryNumber % 2 == 0)
        {
            throw new Exception("something went wrong");
        }

        return DoWorkImpl(workItemId);
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

        Activity.Current?.SetStatus(ActivityStatusCode.Error);

        throw new AggregateException(exceptions!);
    }
}
