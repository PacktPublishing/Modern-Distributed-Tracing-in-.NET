using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace issues.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LockController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public LockController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("load");
        }

        [HttpGet]
        public async Task<string> Lock(CancellationToken cancellationToken)
        {
            var ts = Stopwatch.StartNew();
            await semaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                await _httpClient.GetAsync("/dummy/?delay=100", cancellationToken);
                return $"Done in {ts.ElapsedMilliseconds} ms";
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}