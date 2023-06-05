using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => services
        .AddOpenTelemetry()
        .WithTracing(b => b
            .AddSource("Microsoft.Azure.Functions.Worker")
            .AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString)
            .AddHttpClientInstrumentation())
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddAzureMonitorMetricExporter(o => o
              .ConnectionString = connectionString)
            .AddHttpClientInstrumentation()))
    .Build();

host.Run();