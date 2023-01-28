using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace issues.Controllers;

[ApiController]
[Route("[controller]")]
public class OkController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public OkController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("load");
    }

    [HttpGet]
    public async Task<string> Ok(CancellationToken cancellationToken)
    {
        var ts = Stopwatch.StartNew();
        await _httpClient.GetAsync("/dummy/?delay=10", cancellationToken);
        return $"Done in {ts.ElapsedMilliseconds} ms";
    }
}