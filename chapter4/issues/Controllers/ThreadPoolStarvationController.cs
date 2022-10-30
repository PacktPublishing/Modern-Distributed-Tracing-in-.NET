using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace issues.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ThreadPoolStarvationController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ThreadPoolStarvationController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("load");
        }

        [HttpGet]
        public string ThreadPoolStarvation(CancellationToken cancellationToken)
        {
            var ts = Stopwatch.StartNew();
            _httpClient.GetAsync("/dummy/?delay=100", cancellationToken).Wait();
            return $"Done in {ts.ElapsedMilliseconds} ms";
        }
    }
}