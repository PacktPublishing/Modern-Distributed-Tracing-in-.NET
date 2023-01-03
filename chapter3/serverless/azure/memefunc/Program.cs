using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;

var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker => worker.UseMiddleware<OpenTelemetryMiddleware>())
    .ConfigureServices(services => services
        .AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
            .AddSource(nameof(OpenTelemetryMiddleware))
            .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
            .AddHttpClientInstrumentation())
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
            .AddHttpClientInstrumentation())
        .StartWithHost())
    .Build();

host.Run();

class OpenTelemetryMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly ActivitySource Source = new(nameof(OpenTelemetryMiddleware));
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var activity = Source.StartActivity("worker", ActivityKind.Internal, context.TraceContext.TraceParent);
        await next.Invoke(context);
    }
}

