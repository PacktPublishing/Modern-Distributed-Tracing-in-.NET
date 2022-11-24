using frontend;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageConfig = builder.Configuration.GetSection("Storage");
var storageEndpoint = storageConfig?.GetValue<string>("Endpoint") ?? "http://localhost:5050";
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(storageEndpoint);
});

builder.Services.AddSingleton<StorageService>();

ConfigureTelemetry(builder);

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// return context to the caller with W3C traceresponse (draft specification)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Add("traceresponse", Activity.Current?.Id);
    await next.Invoke();
});


app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddJaegerExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

    builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder => meterProviderBuilder
        .AddPrometheusExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()); 
}