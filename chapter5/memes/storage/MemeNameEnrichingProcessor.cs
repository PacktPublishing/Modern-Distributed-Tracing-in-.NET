using OpenTelemetry;
using System.Diagnostics;

namespace storage;

class MemeNameEnrichingProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.IsAllDataRequested && 
            (Baggage.Current.GetBaggage().TryGetValue("meme_name", out var name) || 
            (name = activity.GetBaggageItem("meme_name")) != null))
        { 
            activity.SetTag("meme_name", name);
        }
    }
}