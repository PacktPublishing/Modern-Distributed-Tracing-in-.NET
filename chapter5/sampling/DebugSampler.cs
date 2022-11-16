using OpenTelemetry.Trace;
using System.Text.RegularExpressions;

namespace sampling;


class DebugSampler : Sampler
{
    private readonly static Sampler AlwaysOnSampler = new AlwaysOnSampler();
    private readonly static Regex DebugFlag = new Regex("(^|,)myapp=debug:1($|,)", RegexOptions.Compiled);

    private readonly Sampler _defaultSampler;
    public DebugSampler(double probability)
    {
        _defaultSampler = new TraceIdRatioBasedSampler(probability);
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var tracestate = samplingParameters.ParentContext.TraceState;
        if (tracestate != null && DebugFlag.IsMatch(tracestate))
        {
            return AlwaysOnSampler.ShouldSample(samplingParameters);
        }

        return _defaultSampler.ShouldSample(samplingParameters);
    }
}