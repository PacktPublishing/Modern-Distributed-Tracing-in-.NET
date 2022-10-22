using frontend;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageEndpoint = builder.Configuration.GetSection("Storage").GetValue<string>("Endpoint");
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(storageEndpoint);
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

    Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());
    
    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder => 
        tracerProviderBuilder
            .AddXRayTraceId()
            .AddOtlpExporter(opt =>
            {
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.Endpoint = new Uri(collectorEndpoint + "/v1/traces");
            })
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation());
}