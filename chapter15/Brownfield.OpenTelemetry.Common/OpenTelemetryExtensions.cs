using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Brownfield.OpenTelemetry.Common;

public static class OpenTelemetryExtensions
{
    public static void ConfigureTelemetry(this WebApplicationBuilder builder, CompatibilityOptions options)
    {
        if (options.SupportLegacyCorrelation)
        {
            Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
                new TextMapPropagator[] {
                new TraceContextPropagator(),
                new BaggagePropagator(),
                new CorrelationIdPropagator(),
                }
            ));
        }

        var resourceBuilder = GetResourceBuilder();

        builder.Services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(resourceBuilder)
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter());

        builder.Logging.AddOpenTelemetry(o =>
        {
            o.ParseStateValues = true;
            o.SetResourceBuilder(resourceBuilder)
                .AddOtlpExporter();
        });
    }

    private static ResourceBuilder GetResourceBuilder()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var serviceName = entryAssembly?.GetName().Name ?? "unknown";
        var serviceVersion = entryAssembly?.GetName()?.Version?.ToString(); ;

        return ResourceBuilder
            .CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName);
    }
}
