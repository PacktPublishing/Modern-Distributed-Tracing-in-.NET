using System.Diagnostics;

namespace events;
public class Worker
{
    private static readonly ActivitySource Source = new ActivitySource("Worker");
    private static readonly HttpClient Client = new (new RateLimitingHandler());

    public static async Task DoWork(int workItemId)
    {
        using var work = Source.StartActivity();
        try
        {
            work?.AddTag("work_item.id", workItemId);
            var res = await Client.GetAsync("https://www.bing.com/search?q=distributed%20tracing", HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();

            work?.AddEvent(new ActivityEvent("received_response_headers"));
            var contents = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"Work item {workItemId} done!");
        }
        catch (Exception ex)
        {
            work?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }
}
