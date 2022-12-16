using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace app.tests;

public class OtherTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    public OtherTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetDocument()
    {
        // we just run some other tests to simulate how parallel tests execution 
        // affects tests (generating multiple unrelated activities on the same processor)
        for (int  i = 0; i < 100; i ++)
        {
            await _client.GetAsync("/document/foo");
            await Task.Delay(10);
        }
    }
}