using System.Diagnostics;

namespace Memes.OpenTelemetry.Common;

public static class ActivityExtensions
{
    public static void AddMemeName(this Activity activity, string memeName)
    {
        if (activity.IsAllDataRequested)
        {
            activity.SetTag(SemanticConventions.MemeNameKey, memeName);
        }
    }
}
