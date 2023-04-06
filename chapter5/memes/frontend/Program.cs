using frontend;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

//XCorrelationIdPropagator.ConfigureCustomPropagatorSample();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageConfig = builder.Configuration.GetSection("Storage");
var storageEndpoint = storageConfig?.GetValue<string>("Endpoint") ?? "http://localhost:5050";
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(storageEndpoint);
    httpClient.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<RetryHandler>();

builder.Services.AddTransient<RetryHandler>();
builder.Services.AddSingleton<StorageService>();

ConfigureTelemetry(builder);

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");
app.UseExceptionHandler("/Error");
// ExceptionHandler handles the exception, so it it's not reported
// by ASP.NET Core instrumentation.
// Let's use our own middleware to record exception as event.
app.UseMiddleware<ExceptionMiddleware>();

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var env = new KeyValuePair<string, object>("env", builder.Environment.EnvironmentName);
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService("frontend", "memes", "1.0.0")
        .AddAttributes(new[] {env});
    var samplingProbability = builder.Configuration.GetSection("Sampling").GetValue<double>("Probability");

    builder.Services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .SetSampler(new TraceIdRatioBasedSampler(samplingProbability))
            .AddProcessor<MemeNameEnrichingProcessor>()
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation(options =>
            {
                options.EnrichWithHttpRequestMessage = (act, req) =>
                {
                    if (req.Options.TryGetValue(new HttpRequestOptionsKey<int>("try"), out var tryCount) && tryCount > 0)
                        act.SetTag("http.resend_count", tryCount);

                    act.SetTag("http.request_content_length", req.Content?.Headers.ContentLength);
                };

                options.EnrichWithHttpResponseMessage = (activity, response) =>
                    activity.SetTag("http.response_content_length", response.Content.Headers.ContentLength);

                options.RecordException = true;
            })
            .AddAspNetCoreInstrumentation(o => o.Filter = ctx => !IsStaticFile(ctx.Request.Path))
            .AddOtlpExporter())
        .WithMetrics(builder => builder
            .AddView("process.runtime.dotnet.jit.il_compiled.size", MetricStreamConfiguration.Drop)
            .AddView("http.server.duration", new MetricStreamConfiguration()
            {
                TagKeys = new[] { "http.host", "http.method", "http.scheme", "http.target", "http.status_code" }
            })
            .SetResourceBuilder(resourceBuilder)
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter())
        .StartWithHost();
}

static bool IsStaticFile(PathString requestPath)
{
    return requestPath.HasValue && requestPath.HasValue &&
        (requestPath.Value.EndsWith(".js") || 
         requestPath.Value.EndsWith(".css"));
}
