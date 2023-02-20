using System.Diagnostics;

namespace events;
public class Worker
{
    private static readonly ActivitySource Source = new ActivitySource("Worker");
    private static readonly HttpClient Client = new (new RateLimitingHandler());

    public static async Task DoWork(int workItemId)
    {
        using var doWork = Source.StartActivity();
        try
        {
            doWork?.AddTag("work_item.id", workItemId);
            var res = await Client.GetAsync("https://www.bing.com/search?q=distributed%20tracing", HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();

            doWork?.AddEvent(new ActivityEvent("received_response_headers"));
            var contents = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"Work item {workItemId} done!");
        }
        catch (Exception ex)
        {
            doWork?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }
}
