using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("load", c => c.BaseAddress = new Uri("http://localhost:5051"));

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
    tracerProviderBuilder
        .SetSampler(new TraceIdRatioBasedSampler(0.01))
        .AddOtlpExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
    meterProviderBuilder
        .AddOtlpExporter()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddEventCountersInstrumentation(opt => opt.AddEventSources("Microsoft-AspNetCore-Server-Kestrel", "Microsoft.AspNetCore.Http.Connections", "System.Net.Http"))
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

var app = builder.Build();

app.MapControllers();

app.Run();
