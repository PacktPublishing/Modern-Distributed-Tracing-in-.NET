using OpenTelemetry;
using System.Diagnostics;

namespace Memes.OpenTelemetry.Common;

class MemeNameEnrichingProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var name = GetName(activity);
        if (name != null) 
            activity.SetTag(SemanticConventions.MemeNameKey, name);
    }

    private static string? GetName(Activity activity)
    {
        if (Baggage.Current.GetBaggage().TryGetValue(SemanticConventions.MemeNameKey, out var name)) 
            return name;

        return activity.GetBaggageItem(SemanticConventions.MemeNameKey);
    }
}