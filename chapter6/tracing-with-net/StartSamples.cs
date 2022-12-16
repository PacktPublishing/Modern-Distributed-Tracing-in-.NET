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

    public static async Task IsAllDataRequested()
    {
        using var activity = Source.StartActivity(ActivityKind.Server);
        if (activity?.IsAllDataRequested == true)
            activity?.SetTag("foo", GetValue());

        await DoWork();
    }

    private static async Task DoWork()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));
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

}
