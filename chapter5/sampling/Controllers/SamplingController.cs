using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace sampling.Controllers;

[ApiController]
[Route("[controller]")]
public class SamplingController : ControllerBase
{
    public record SamplingInfo(bool? isAllDataRequested, bool? isRecorded, string? incomingTraceparent, string outgoingTraceparent);

    private readonly HttpClient _httpClient;

    public SamplingController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("dummy");
    }

    [HttpGet]
    public async Task<SamplingInfo> Get()
    {
        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "/dummy/?delay=1");
        await _httpClient.SendAsync(outgoingRequest);

        Request.Headers.TryGetValue("traceparent", out var incomingTraceParent);
        var outgoingTraceParent = outgoingRequest.Headers.GetValues("traceparent").Single();

        return new SamplingInfo(Activity.Current?.IsAllDataRequested, Activity.Current?.Recorded, incomingTraceParent, outgoingTraceParent);
    }
}
