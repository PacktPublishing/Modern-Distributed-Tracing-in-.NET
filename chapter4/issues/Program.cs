using issues;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
workerThreads = 16;
ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProcessingQueue>();
builder.Services.AddHostedService<ProcessingQueue>();
builder.Services.AddControllers();
builder.Services.AddHttpClient("load", c => c.BaseAddress = new Uri("http://localhost:5051"));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .SetSampler(new TraceIdRatioBasedSampler(0.00001))
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