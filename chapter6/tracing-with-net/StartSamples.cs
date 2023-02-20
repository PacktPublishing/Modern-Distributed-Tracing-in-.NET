using System.Diagnostics;
using System.Text;

namespace tracing_with_net;

internal class StartSamples
{
    private static readonly ActivitySource Source = new("Worker");

    public static async Task RunAll()
    {
        await Create();
        await StartRoot();
        await StartFromParentId();
        await StartFromParentContext();
        await InjectContext();
        await IsAllDataRequested();
    }

    public static async Task Create()
    {
        var activity = Source.CreateActivity("foo", ActivityKind.Client)?
            .Start();

        await DoWork();
        
        activity?.Stop();
    }

    public static async Task StartRoot()
    {
        using var active = Source.StartActivity();
        
        Activity.Current = null;
        
        var root = Source.StartActivity("root", ActivityKind.Server);
        Debug.Assert(root!.Parent == null);
        await DoWork();
        root?.Stop();

        Activity.Current = active;
    }


    public static async Task StartFromParentId()
    {
        var parentId = "00-782d793e810fc6b110660f644f907ae3-6a6372ff1a9b4f5e-01";
        using var activity = Source.StartActivity("from parent-id", ActivityKind.Server, parentId);
        await DoWork();
        Debug.Assert(activity?.ParentId == parentId);
    }


    private static void ExtractContext(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) {
        fieldValues = null;
        fieldValue = null;
        if (carrier is Dictionary<string, string> dictionary)
        {
            dictionary.TryGetValue(fieldName, out fieldValue);
        }
    }

    public static async Task StartFromParentContext()
    {
        Dictionary<string, string> headers = new (){
            {"traceparent", "00-782d793e810fc6b110660f644f907ae3-6a6372ff1a9b4f5e-01"},
            {"tracestate", "foo=bar"}
        };

        DistributedContextPropagator.Current.ExtractTraceIdAndState(headers, ExtractContext, out var traceparent, out var tracestate);
        ActivityContext.TryParse(traceparent, tracestate, true, out var parentContext);
        using var activity = Source.StartActivity("from parent context", ActivityKind.Server, parentContext);
        await DoWork();
        Debug.Assert(activity?.ParentId == headers["traceparent"]);
        Debug.Assert(activity?.TraceStateString == headers["tracestate"]);
        Debug.Assert(activity?.HasRemoteParent == true);
    }

    private static void InjectContext(object? carrier, string key, string value)
    {
        if (carrier is Dictionary<string, string> dictionary)
        {
            dictionary.Add(key, value);
        }
    }

    public static async Task InjectContext()
    {
        Dictionary<string, string> headers = new();

        using var activity = Source.StartActivity("inject context", ActivityKind.Server)!;
        activity.TraceStateString = "foo=bar";
        await DoWork();
        DistributedContextPropagator.Current.Inject(activity, headers, InjectContext);
        Debug.Assert(headers["traceparent"] == activity.Id);
        Debug.Assert(headers["tracestate"] == "foo=bar");
    }

    public static async Task IsAllDataRequested()
    {
        using var activity = Source.StartActivity(ActivityKind.Server);
        if (activity?.IsAllDataRequested == true)
            activity?.SetTag("foo", GetValue());

        await DoWork();
    }

    private static string GetValue()
    {
        var sb = new StringBuilder();
        sb.Append("some")
          .Append(" ")
          .Append("expensive")
          .Append(" ")
          .Append("operation");

        return sb.ToString();
    }

    private static async Task DoWork()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));
    }

}
