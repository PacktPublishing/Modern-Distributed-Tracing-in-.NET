using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Memes.OpenTelemetry.Common;

public static class OpenTelemetryExtensions
{
    public static void ConfigureTelemetry(this WebApplicationBuilder builder, MemesTelemetryConfiguration config)
    {
        var resourceBuilder = GetResourceBuilder(config);
        var sampler = GetSampler(config.SamplingStrategy, config.SamplingProbability);
        builder.Services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetSampler(sampler)
                .AddProcessor<MemeNameEnrichingProcessor>()
                .SetResourceBuilder(resourceBuilder)
                .AddHttpClientInstrumentation(o => o.ConfigureHttpClientCollection(config.RecordHttpExceptions))
                .AddAspNetCoreInstrumentation(o => o.ConfigureAspNetCoreCollection(config.RecordHttpExceptions, config.AspNetCoreRequestFilter))
                .AddCustomInstrumentations(config.ConfigureTracing)
                .AddOtlpExporter())
            .WithMetrics(builder => builder
                .SetResourceBuilder(resourceBuilder)
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddCustomInstrumentations(config.ConfigureMetrics)
                .AddOtlpExporter());

        builder.Logging.AddOpenTelemetry(o => 
        {
            o.ParseStateValues = true;
            o.SetResourceBuilder(resourceBuilder)
             .AddOtlpExporter();
        });
    }

    private static ResourceBuilder GetResourceBuilder(MemesTelemetryConfiguration config)
    {
        return ResourceBuilder
            .CreateDefault()
            .AddService(config.ServiceName, config.Namespace, config.ServiceVersion, false, config.ServiceInstanceId)
            .AddAttributes(config.ResourceAttributes);

    }
    private static TracerProviderBuilder AddCustomInstrumentations(this TracerProviderBuilder builder, Func<TracerProviderBuilder, TracerProviderBuilder>? config) =>  config != null ? config(builder) : builder;
    private static MeterProviderBuilder AddCustomInstrumentations(this MeterProviderBuilder builder, Func<MeterProviderBuilder, MeterProviderBuilder>? config) => config != null ? config(builder) : builder;

    private static void ConfigureHttpClientCollection(this HttpClientInstrumentationOptions options, bool recordExceptions)
    {
        options.EnrichWithHttpRequestMessage = (act, req) =>
        {
            if (req.Options.TryGetValue(new HttpRequestOptionsKey<int>(SemanticConventions.HttpResendCountKey), out var tryCount) && tryCount > 0)
                act.SetTag(SemanticConventions.HttpResendCountKey, tryCount);

            act.SetTag(SemanticConventions.HttpRequestLengthKey, req.Content?.Headers.ContentLength);
        };

        options.EnrichWithHttpResponseMessage = (activity, response) =>
            activity.SetTag(SemanticConventions.HttpResponseLengthKey, response.Content.Headers.ContentLength);

        options.RecordException = recordExceptions;
    }

    private static void ConfigureAspNetCoreCollection(this AspNetCoreInstrumentationOptions options, bool recordExceptions, Func<HttpContext, bool>? filter)
    {
        options.RecordException = recordExceptions;
        if (filter != null)
        {
            options.Filter = filter;
        }
    }

    private static Sampler GetSampler(SamplingStrategy strategy, double probability)
    {
        var probabilitySampler = new TraceIdRatioBasedSampler(probability);
        return strategy switch
        {
            SamplingStrategy.Parent => new ParentBasedSampler(probabilitySampler),
            SamplingStrategy.Probability => probabilitySampler,
            _ => new AlwaysOffSampler(),
        };
    }
}
