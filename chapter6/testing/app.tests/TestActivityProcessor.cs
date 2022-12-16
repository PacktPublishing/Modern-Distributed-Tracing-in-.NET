using OpenTelemetry;
using OpenTelemetry.Resources;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace app.tests;

public class TestActivityProcessor : BaseProcessor<Activity>
{
    private readonly ConcurrentQueue<Activity> _processed = new ();

    public override void OnEnd(Activity activity) => _processed.Enqueue(activity);
    public List<Activity> ProcessedActivities => _processed.ToList();
    public Resource? Resource => ParentProvider?.GetResource();
}
