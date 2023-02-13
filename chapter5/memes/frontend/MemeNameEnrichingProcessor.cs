using OpenTelemetry;
using System.Diagnostics;

namespace frontend;

class MemeNameEnrichingProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var name = GetName(activity);
        if (name != null) 
            activity.SetTag("meme_name", name);
    }

    private string? GetName(Activity activity)
    {
        if (Baggage.Current.GetBaggage().TryGetValue("meme_name", out var name)) 
            return name;

        return activity.GetBaggageItem("meme_name");
    }
}