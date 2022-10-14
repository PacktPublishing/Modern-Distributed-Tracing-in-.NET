using frontend;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var daprUrl = builder.Configuration.GetSection("Dapr")?["Endpoint"] ?? "http://localhost:3500";
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(daprUrl);
    httpClient.DefaultRequestHeaders.Add("dapr-app-id", "storage");
});

builder.Services.AddSingleton<StorageService>();

ConfigureTelemetry(builder);

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
    var collectorEndpoint = builder.Configuration.GetSection("OtelCollector")?.GetValue<string>("Endpoint");
    if (collectorEndpoint == null) 
    {
        return;
    }

    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        tracerProviderBuilder.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/traces");
        })
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());


    builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
        meterProviderBuilder.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/metrics");
        })
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());
        
    builder.Logging.AddOpenTelemetry(options =>
        options.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/logs");
        }));
}