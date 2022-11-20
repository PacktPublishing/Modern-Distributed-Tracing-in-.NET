using frontend;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ConfigureCustomPropagator();

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
    var envAttribute = new KeyValuePair<string, object>("env", builder.Environment.EnvironmentName);
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService("frontend", "memes", "1.0.0")
        .AddAttributes(new[] {envAttribute});
    var samplingProbability = builder.Configuration.GetSection("Sampling").GetValue<double>("Probability");

    builder.Services.AddOpenTelemetryTracing(builder => builder
        .SetSampler(new TraceIdRatioBasedSampler(samplingProbability))
        .AddProcessor<MemeNameEnrichingProcessor>()
        .SetResourceBuilder(resourceBuilder)
        .AddHttpClientInstrumentation(options =>
        {
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                if (request.Options.TryGetValue(new HttpRequestOptionsKey<int>("try"), out var retryCount) && retryCount > 0)
                    activity.SetTag("http.resend_count", retryCount);

                activity.SetTag("http.request_content_length", request.Content?.Headers.ContentLength);
            };

            options.EnrichWithHttpResponseMessage = (activity, response) =>
                activity.SetTag("http.response_content_length", response.Content.Headers.ContentLength);

            options.RecordException = true;
        })
        .AddAspNetCoreInstrumentation(o => o.Filter = ctx => !IsStaticFile(ctx.Request.Path))
        .AddOtlpExporter());

    builder.Services.AddOpenTelemetryMetrics(builder => builder
        .AddView("process.runtime.dotnet.jit.il_compiled.size", MetricStreamConfiguration.Drop)
        .AddView("http.server.duration", new MetricStreamConfiguration()
        {
            TagKeys = new[] { "http.host", "http.method", "http.scheme", "http.target", "http.status_code" }
        })
         .SetResourceBuilder(resourceBuilder)
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
}

static bool IsStaticFile(PathString requestPath)
{
    return requestPath.HasValue && requestPath.HasValue &&
        (requestPath.Value.EndsWith(".js") || 
         requestPath.Value.EndsWith(".css"));
}
