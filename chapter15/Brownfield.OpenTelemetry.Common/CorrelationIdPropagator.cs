using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace Brownfield.OpenTelemetry.Common;

public class CorrelationIdPropagator : TextMapPropagator
{
    private const string CorrelationIdHeaderName = "correlation-id";
    private static readonly ISet<string> fields = new HashSet<string>() { CorrelationIdHeaderName };
    public override ISet<string> Fields => fields;

    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        if (context.ActivityContext.IsValid()) return context;

        var correlationIds = getter.Invoke(carrier, CorrelationIdHeaderName);

        // check if correlation-id can be transformed into a valid trace-id
        if (TryGetTraceId(correlationIds, out var traceId))
        {
            var traceContext = new ActivityContext(ActivityTraceId.CreateFromString(traceId), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, isRemote: true);
            return new PropagationContext(traceContext, context.Baggage);
        }
        else
        {
            // log original correlation-id
        }


        return context;
    }

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {
        if (context.ActivityContext.IsValid())
        {
            setter.Invoke(carrier, CorrelationIdHeaderName, context.ActivityContext.TraceId.ToString());
        }
    }

    private bool TryGetTraceId(IEnumerable<string>? correlationIds, out string? traceId)
    {
        traceId = null;
        if (correlationIds == null || !correlationIds.Any() ||
            string.IsNullOrEmpty(correlationIds.First())) return false;

        var correlationId = correlationIds.First();

        traceId = correlationId.Replace("-", "");

        if (correlationId.Length < 32) traceId = traceId.PadRight(32, '0');
        else traceId = traceId.Substring(0, 32);

        return true;
    }
}
