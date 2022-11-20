using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace frontend;

public class XCorrelationIdPropagator : TextMapPropagator
{
    private readonly static ISet<string> fields = new HashSet<string>() { "x-correlation-id" };
    public override ISet<string> Fields => fields;

    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var correlationId = getter.Invoke(carrier, "x-correlation-id")?.FirstOrDefault();
        if (Guid.TryParse(correlationId, out var guid))
        {
            var activityContext = new ActivityContext(
                ActivityTraceId.CreateFromString(guid.ToString("n")),
                ActivitySpanId.CreateRandom(), 
                context.ActivityContext.TraceFlags,
                context.ActivityContext.TraceState,
                true);
            return new PropagationContext(activityContext, context.Baggage);
        } 

        return context;
    }

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {
    }


    public static void ConfigureCustomPropagatorSample()
    {
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[] {
        new OpenTelemetry.Extensions.Propagators.B3Propagator(true),
        new XCorrelationIdPropagator(),
        new BaggagePropagator() }));

        DistributedContextPropagator.Current =
            DistributedContextPropagator.CreateNoOutputPropagator();
    }
}
