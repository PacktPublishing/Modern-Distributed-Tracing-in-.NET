using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine;

Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(r => r.AddService("load"))
    .SetSampler(new TraceIdRatioBasedSampler(0.001))
    .AddJaegerExporter()
    .AddHttpClientInstrumentation()
    .Build();

var parallelRequestsOption = new Option<int>("--parallel", () => 100, "parallel requests");
var lockOption = new Option<bool>("--sync", () => false, "test sync lock");

var lockCommand = new Command("lock") { parallelRequestsOption, lockOption };
lockCommand.SetHandler((parallelRequests, syncLock) => Loop(() => "http://localhost:5051/lock?sync=" + syncLock, parallelRequests), parallelRequestsOption, lockOption);

var memLeakCommand = new Command("memory-leak") { parallelRequestsOption };
memLeakCommand.SetHandler((parallelRequests) => Loop(() => "http://localhost:5051/memoryleak", parallelRequests), parallelRequestsOption);

var starveCommand = new Command("starve") { parallelRequestsOption };
starveCommand.SetHandler((parallelRequests) => Loop(() => "http://localhost:5051/threadpoolstarvation", parallelRequests), parallelRequestsOption);

var okCommand = new Command("ok") { parallelRequestsOption };
okCommand.SetHandler((parallelRequests) => Loop(() => "http://localhost:5051/ok", parallelRequests), parallelRequestsOption);

var spinCommand = new Command("spin") { parallelRequestsOption };
spinCommand.SetHandler((parallelRequests) =>
{
    var nums = new int[parallelRequests];
    var rand = new Random();

    for (int i = 0; i < nums.Length; i++)
    {
        nums[i] = rand.Next(30, 40);
    }

    long roundRobin = 0;
    return Loop(() => "http://localhost:5051/spin?fib=" + nums[roundRobin++ % parallelRequests], parallelRequests);
}, parallelRequestsOption);

var command = new RootCommand("Load generator");
command.AddCommand(spinCommand);
command.AddCommand(lockCommand);
command.AddCommand(memLeakCommand);
command.AddCommand(starveCommand);
command.AddCommand(okCommand);
await command.InvokeAsync(args);

static async Task Loop(Func<string> getEndpoint, int parallelRequests)
{
    var tasks = new HashSet<Task<HttpResponseMessage>>();
    var client = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    while (true)
    {
        while (tasks.Count < parallelRequests)
        {
            tasks.Add(client.GetAsync(getEndpoint()));
        }

        var t = await Task.WhenAny(tasks);
        tasks.Remove(t);

        try
        {
            t.Result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}