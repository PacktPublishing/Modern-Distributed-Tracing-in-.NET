using OpenTelemetry.Trace;
using System.Text.RegularExpressions;

namespace sampling;


class DebugSampler : Sampler
{
    private readonly static Sampler On = new AlwaysOnSampler();
    private readonly static Regex DebugFlag = new Regex("(^|,)myapp=debug:1($|,)", RegexOptions.Compiled);

    private readonly Sampler _default;
    public DebugSampler(double probability)
    {
        _default = new TraceIdRatioBasedSampler(probability);
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        var tracestate = parameters.ParentContext.TraceState;
        if (tracestate != null && DebugFlag.IsMatch(tracestate))
        {
            return On.ShouldSample(parameters);
        }

        return _default.ShouldSample(parameters);
    }
}