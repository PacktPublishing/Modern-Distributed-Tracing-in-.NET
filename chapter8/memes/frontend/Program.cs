using frontend;
using OpenTelemetry.Logs;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

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

app.Logger.LogInformation("configuration: {storageEndpoint}", storageEndpoint);

app.UseStatusCodePagesWithRedirects("/errors/{0}");
app.UseExceptionHandler("/Error");

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Application stopping"));
app.Lifetime.ApplicationStopped.Register(() => app.Logger.LogInformation("Application stopped"));

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var resource = ResourceBuilder.CreateDefault()
        .AddAttributes(new [] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName) });

    builder.Services
        .AddOpenTelemetry()
        .WithTracing(b => b
            .SetSampler(new TraceIdRatioBasedSampler(0.5))
            .SetResourceBuilder(resource)
            .AddSource("Storage")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter())
    .StartWithHost();

    builder.Logging
        .AddOpenTelemetry(b => {
            b.SetResourceBuilder(resource);
            b.ParseStateValues = true;
            b.AddOtlpExporter();
        });
}