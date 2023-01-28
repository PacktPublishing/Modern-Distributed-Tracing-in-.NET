using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace issues.Controllers;

[ApiController]
[Route("[controller]")]
public class LockController : ControllerBase
{
    private readonly static List<string> notThreadSafeData = new List<string>();
    private readonly static object lck = new object();
    private readonly HttpClient _httpClient;
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    public LockController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("load");
    }

    [HttpGet]
    public async Task<string> Lock([FromQuery]bool? sync, CancellationToken token)
    {
        var ts = Stopwatch.StartNew();
        if (sync.GetValueOrDefault(false))
        {
            lock(lck)
            {
                ThreadUnsafeOperation();
            }
            await _httpClient.GetAsync("/dummy/?delay=10", token);
        }
        else
        {
            await semaphoreSlim.WaitAsync(token);
            try
            {
                ThreadUnsafeOperation();
                await _httpClient.GetAsync("/dummy/?delay=10", token);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        return $"Done in {ts.ElapsedMilliseconds} ms";
    }

    private void ThreadUnsafeOperation()
    {
        if (notThreadSafeData.Count > 100)
        {
            notThreadSafeData.Clear();
        }

        notThreadSafeData.Add(Guid.NewGuid().ToString());
    }
}