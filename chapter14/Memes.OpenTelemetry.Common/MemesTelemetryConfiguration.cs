using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Memes.OpenTelemetry.Common;

public class MemesTelemetryConfiguration
{
    public string ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? ServiceInstanceId { get; set; }
    public string? Namespace { get; set; }
    public string? EnvironmentName { get; set; }

    public IEnumerable<KeyValuePair<string, object>> ResourceAttributes { get; }
    public SamplingStrategy SamplingStrategy { get; set; } = SamplingStrategy.Probability;
    public double SamplingProbability { get; set; } = 0.001;
    public Func<HttpContext, bool>? AspNetCoreRequestFilter { get; set; } = _ => true;
    public Func<TracerProviderBuilder, TracerProviderBuilder>? ConfigureTracing { get; set; }
    public Func<MeterProviderBuilder, MeterProviderBuilder>? ConfigureMetrics { get; set; }
    public bool RecordHttpExceptions { get; set; } = true;
    public MemesTelemetryConfiguration()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        ServiceName = entryAssembly?.GetName().Name ?? "unknown";
        ServiceVersion = entryAssembly?.GetName()?.Version?.ToString();
        ServiceInstanceId = Environment.MachineName;
        Namespace = "memes";

        var resourceAttributes = new List<KeyValuePair<string, object>>();
        if (EnvironmentName != null)
        {
            resourceAttributes.Add(new (SemanticConventions.EnvironmentKey, EnvironmentName));
        }

        ResourceAttributes = resourceAttributes;
    }
}
