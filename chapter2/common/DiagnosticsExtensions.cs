using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace common;

public static class DiagnosticsExtensions
{
    public static TracerProviderBuilder ConfigureTracing(this TracerProviderBuilder tracerProviderBuilder, string collectorEndpoint)
    {
        return tracerProviderBuilder
             .AddOtlpExporter(opt =>
             {
                 opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                 opt.Endpoint = new Uri(collectorEndpoint + "/v1/traces");
             })
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    }

    public static MeterProviderBuilder ConfigureMetrics(this MeterProviderBuilder meterProviderBuilder, string collectorEndpoint)
    {
        return meterProviderBuilder
            .AddOtlpExporter(opt =>
            {
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.Endpoint = new Uri(collectorEndpoint + "/v1/metrics");
            })
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    }

    public static OpenTelemetryLoggerOptions ConfigureLogs(this OpenTelemetryLoggerOptions options, string collectorEndpoint)
    {
        return options.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/logs");
        });
    }
}
