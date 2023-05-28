using Microsoft.AspNetCore.Mvc;

namespace a.Controllers;

[ApiController]
[Route("[controller]")]
public class AController : ControllerBase
{
    private readonly HttpClient _serviceB;
    
    public AController(IHttpClientFactory httpClientFactory)
    {
        _serviceB = httpClientFactory.CreateClient("b");
    }

    [HttpGet]
    public async Task<string> Get(string to)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/" + to);
        request.Headers.Host = "localhost";
        var response = await _serviceB.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
