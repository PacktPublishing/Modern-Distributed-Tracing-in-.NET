using issues;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProcessingQueue>();
builder.Services.AddHostedService<ProcessingQueue>();
builder.Services.AddControllers();
builder.Services.AddHttpClient("load", c => c.BaseAddress = new Uri("http://localhost:5051"));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .SetSampler(new TraceIdRatioBasedSampler(0.01))
            .AddOtlpExporter()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddOtlpExporter()
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation());

var app = builder.Build();
app.MapControllers();

app.Run();