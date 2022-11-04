using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Numerics;

namespace issues.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SpinController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public SpinController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("load");
        }

        [HttpGet]
        public async Task<string> Spin([FromQuery] int fib, CancellationToken cancellationToken)
        {
            var ts = Stopwatch.StartNew();
            await _httpClient.GetAsync("/dummy/?delay=100", cancellationToken);
            var result = MostInefficientFibonacci(fib);
            return $"Done in {ts.ElapsedMilliseconds} ms, '{fib}' fibonacci number is {result}";
        }

        private BigInteger MostInefficientFibonacci(int n)
        {
            if (n == 0) return 0;
            if (n == 1) return 1;

            return MostInefficientFibonacci(n - 1) + MostInefficientFibonacci(n - 2);
        }
    }
}