using issues;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProcessingQueue>();
builder.Services.AddHostedService<ProcessingQueue>();
builder.Services.AddControllers();
builder.Services.AddHttpClient("load", c => c.BaseAddress = new Uri("http://localhost:5051"));

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
    tracerProviderBuilder
        .SetSampler(new TraceIdRatioBasedSampler(0.001))
        .AddOtlpExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
    meterProviderBuilder
        .AddOtlpExporter()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

var app = builder.Build();

app.MapControllers();

app.Run();