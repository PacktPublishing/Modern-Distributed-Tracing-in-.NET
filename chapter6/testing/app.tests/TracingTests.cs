using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using Xunit;

namespace app.tests;

public class TracingTests
    : IClassFixture<TestFactory>, IDisposable
{
    private readonly static ActivitySource TestSource = new("Test");
    private readonly TestFactory _factory;
    private readonly Activity _testActivity;
    private readonly HttpClient _testClient;

    public TracingTests(TestFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _testClient = _factory.CreateClient();
        _testActivity = TestSource.StartActivity("test")!;
    }

    public void Dispose()
    {
        _testActivity.Dispose();
    }

    [Fact]
    public void CheckResource()
    {
        var resource = TestFactory.Processor.Resource;
        Assert.NotNull(resource);
        var serviceNameKvp = resource.Attributes.Single(s => s.Key == "service.name");
        Assert.Equal("testing-sample", serviceNameKvp.Value);
    }
    
    [Fact]
    public async Task GetDocument()
    {
        await _factory.Storage.WriteAsync("foo", "hi");

        var response = await _testClient.SendAsync(CreateGetRequest("/document/foo"));

        Assert.Equal("hi", await response.Content.ReadAsStringAsync());
        var related = GetRelatedActivities();
        Assert.Equal(2, related.Count);

        CheckDocumentActivity(related[0], "foo", "hi".Length, false, false);
        // we know for sure that ASP.NET Core activity is the last to finish
        CheckAspNetCoreActivity(related[1], "GET", "/document/foo", 200);
        Assert.Same(related[0].Parent, related[1]);
        Assert.Equal(_testActivity.Id, related[1].ParentId);
    }
    
    [Fact]
    public async Task NotFoundDocumentFullExample()
    {
        using var testActivity = TestSource.StartActivity("test")!;
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/document/foo");
        request.Headers.Add("traceparent", testActivity.Id);
        var response = await _testClient.SendAsync(request);
       
        var related = TestFactory.Processor.ProcessedActivities
            .Where(a => a.TraceId == _testActivity.TraceId).ToArray();

        // testActivity is still running, so we don't see it here
        Assert.Equal(2, related.Length);

        var document = related[0];
        var httpIn = related[1];
        Assert.Equal("GetDocument", document.OperationName);
        Assert.Equal("foo", document.GetTagItem("document.name"));
        Assert.Null(document.GetTagItem("document.size"));
        Assert.Equal(ActivityStatusCode.Unset, document.Status);
        Assert.Single(document.Events.Where(e => e.Name.Equals("cache_miss")));

        Assert.Equal("/document/foo", httpIn.GetTagItem("http.target"));
        Assert.Equal(404, httpIn.GetTagItem("http.status_code"));
        Assert.Equal(ActivityStatusCode.Unset, httpIn.Status);
        Assert.Empty(httpIn.Events);

        Assert.Same(document.Parent, httpIn);
        Assert.Equal(testActivity.Id, httpIn.ParentId);
    }

    [Fact]
    public async Task NotFoundDocument()
    {
        var response = await _testClient.SendAsync(CreateGetRequest("/document/foo"));
        Assert.Equal(404, (int)response.StatusCode);

        var related = GetRelatedActivities();
        Assert.Equal(2, related.Count);

        CheckDocumentActivity(related[0], "foo", null, false, false);
        CheckAspNetCoreActivity(related[1], "GET", "/document/foo", 404);
    }

    [Fact]
    public async Task ExceptionGettingDocument()
    {
        var response = await _testClient.SendAsync(CreateGetRequest("/document/throw"));
        Assert.Equal(500, (int)response.StatusCode);

        var related = GetRelatedActivities();
        Assert.Equal(2, related.Count);

        CheckDocumentActivity(related[0], "throw", null, false, true);
        CheckAspNetCoreActivity(related[1], "GET", "/document/throw", 500);
    }

    [Fact]
    public async Task GetFromCache()
    {
        _factory.Cache.Set("foo", "bar");

        var response = await _testClient.SendAsync(CreateGetRequest("/document/foo"));
        Assert.Equal(200, (int)response.StatusCode);

        var related = GetRelatedActivities();
        Assert.Equal(2, related.Count);

        CheckDocumentActivity(related[0], "foo", "bar".Length, true, false);
        CheckAspNetCoreActivity(related[1], "GET", "/document/foo", 200);
    }

    private static void CheckAspNetCoreActivity(Activity actual, string httpMethod, string target, int statusCode)
    {
        Assert.Equal(httpMethod, actual.GetTagItem("http.method"));
        Assert.Equal(target, actual.GetTagItem("http.target"));
        Assert.Equal(statusCode, actual.GetTagItem("http.status_code"));
    }

    private static void CheckDocumentActivity(Activity actual, string name, int? size, bool expectCacheHit, bool expectException)
    {
        Assert.Equal("GetDocument", actual.OperationName);
        Assert.Equal(name, actual.GetTagItem("document.name"));
        Assert.Equal(size, actual.GetTagItem("document.size"));
        Assert.Equal(expectException ? ActivityStatusCode.Error : ActivityStatusCode.Unset, actual.Status);
        if (expectCacheHit)
        {
            Assert.Single(actual.Events.Where(e => e.Name.Equals("cache_hit")));
        }
        else
        {
            Assert.Single(actual.Events.Where(e => e.Name.Equals("cache_miss")));
        }

        Assert.Equal(expectException ? 1 : 0, actual.Events.Where(e => e.Name.Equals("exception")).Count());
    }

    private HttpRequestMessage CreateGetRequest(string path)
    {
        // ASP.NET Core test HttpClient is special and is not instrumented
        // so we need to help it propagate test context to
        // the service under test
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("traceparent", _testActivity.Id);
        return request;
    }

    private List<Activity> GetRelatedActivities()
    {
        var allExported = TestFactory.Processor.ProcessedActivities;

        // with parallel test execution, we'll get exported activities from all tests
        // so we need to filter only to sucessors of our activity.
        return allExported.Where(a => a.TraceId == _testActivity.TraceId).ToList();
    }
}