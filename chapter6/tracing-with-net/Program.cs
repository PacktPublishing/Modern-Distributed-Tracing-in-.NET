using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine;
using System.Diagnostics;
using tracing_with_net;

var scenarioOption = new Option<string>("--scenario", () => "basic", "Run OpenTelemetry scenario: basic, with-retries, start-samples, event-samples");

var otelExample = new Command("open-telemetry") { scenarioOption };
otelExample.SetHandler(RunOTelExample, scenarioOption);

var activityListenerExample = new Command("activity-listener");
activityListenerExample.SetHandler(RunActivityListenerExample);

var command = new RootCommand("Tracing instrumentation with .NET")
{
    otelExample,
    activityListenerExample
};
await command.InvokeAsync(args);

static async Task RunOTelExample(string scenario)
{
    using var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .ConfigureResource(b => b.AddService("activity-sample"))
        .AddSource("Worker")
        .AddJaegerExporter()
        .AddConsoleExporter()
        .Build()!;

    if (scenario == "basic")
    {
        await Worker.DoWorkWithBasicTracing(1);
    }
    else if (scenario == "with-retries")
    {
        await Worker.DoWork(new WorkItem(2, "add_user"));
    }
    else if (scenario == "start-samples")
    { 
        await StartSamples.RunAll();
    }
    else
    {
        throw new ArgumentException("Unknown scenario", paramName: nameof(scenario));
    }
}

static async Task RunActivityListenerExample()
{
    ActivitySource.AddActivityListener(new ActivityListener()
    {
        ActivityStopped = PrintActivity,
        ShouldListenTo = source => source.Name == "Worker",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded
    });

    await Worker.DoWorkWithBasicTracing(1);
}

static void PrintActivity(Activity activity)
{
    Console.WriteLine($"{activity.DisplayName}: Id = {activity.Id}, Duration={activity.Duration.TotalMilliseconds}ms, Status = {activity.Status}");
}


/*var queue = new WorkQueue();

queue.Start();

queue.TryEnqueue(new WorkItem(workItemId++, "Sasha", "create"));
queue.TryEnqueue(new WorkItem(workItemId++, "Nick", "update"));

await queue.Stop();
tracerProvider.Shutdown();*/
