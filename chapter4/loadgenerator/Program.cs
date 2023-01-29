using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine;

Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(r => r.AddService("load"))
    .SetSampler(new TraceIdRatioBasedSampler(0.01))
    .AddJaegerExporter()
    .AddHttpClientInstrumentation()
    .Build();

var command = new RootCommand("Load generator");

var countOption = new Option<long>("--count", () => -1, "Total request count, set -1 to run endlessly");
var parallelRequestsOption = new Option<int>("--parallel", () => 100, "Parallel requests");
command.AddGlobalOption(parallelRequestsOption);
command.AddGlobalOption(countOption);

var lockOption = new Option<bool>("--sync", () => false, "test sync lock");
var lockCommand = new Command("lock") { lockOption };

var memLeakCommand = new Command("memory-leak");
var starveCommand = new Command("starve");
var okCommand = new Command("ok");
var spinCommand = new Command("spin") { parallelRequestsOption };

command.AddCommand(okCommand);
command.AddCommand(lockCommand);
command.AddCommand(memLeakCommand);
command.AddCommand(starveCommand);
command.AddCommand(spinCommand);

okCommand.SetHandler((parallelRequests, count) => Loop("http://localhost:5051/ok", parallelRequests, count), parallelRequestsOption, countOption);
lockCommand.SetHandler((parallelRequests, syncLock, count) => Loop("http://localhost:5051/lock?sync=" + syncLock, parallelRequests, count), parallelRequestsOption, lockOption, countOption);
memLeakCommand.SetHandler((parallelRequests, count) => Loop("http://localhost:5051/memoryleak", parallelRequests, count), parallelRequestsOption, countOption);
starveCommand.SetHandler((parallelRequests, count) => Loop("http://localhost:5051/threadpoolstarvation", parallelRequests, count), parallelRequestsOption, countOption);
spinCommand.SetHandler((parallelRequests, count) =>
{
    var nums = new int[parallelRequests];
    var rand = new Random();

    for (int i = 0; i < nums.Length; i++)
    {
        nums[i] = rand.Next(30, 40);
    }

    long roundRobin = 0;
    return Loop("http://localhost:5051/spin?fib=" + nums[roundRobin++ % parallelRequests], parallelRequests, count);
}, parallelRequestsOption, countOption);

await command.InvokeAsync(args);

static async Task Loop(string endpoint, int parallelRequests, long count)
{
    var tasks = new HashSet<Task<HttpResponseMessage>>();
    var client = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    if (count < 0) count = long.MaxValue;
    for (long i = 0; i < count; i ++)
    {
        while (tasks.Count < parallelRequests)
        {
            tasks.Add(client.GetAsync(endpoint));
        }

        var t = await Task.WhenAny(tasks);
        tasks.Remove(t);

        try
        {
            t.Result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}