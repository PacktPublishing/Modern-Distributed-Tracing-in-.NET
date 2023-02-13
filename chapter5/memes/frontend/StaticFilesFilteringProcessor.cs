using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace frontend;

public class StaticFilesFilteringProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    { 
        if (activity.Kind == ActivityKind.Server &&
            activity.GetTagItem("http.method") as string == "GET" && 
            activity.GetTagItem("http.route") == null)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }

    static void StaticFilesProcessorSample(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetSampler(new TraceIdRatioBasedSampler(1))
                .AddProcessor<StaticFilesFilteringProcessor>()
                .AddProcessor<MemeNameEnrichingProcessor>()
                .AddHttpClientInstrumentation(o => o.RecordException = true)
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter());
    }
}
