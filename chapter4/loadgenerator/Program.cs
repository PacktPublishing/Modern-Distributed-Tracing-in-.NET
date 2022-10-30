using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

int parallelRequests = 100;
ThreadPool.SetMinThreads(parallelRequests, parallelRequests);

Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(r => r.AddService("load"))
    .SetSampler(new TraceIdRatioBasedSampler(0.01))
    .AddJaegerExporter()
    .AddHttpClientInstrumentation()
    .Build();

var client = new HttpClient();
var tasks = new HashSet<Task>();

var nums = new int[parallelRequests];
var rand = new Random();
for (int i = 0; i < nums.Length; i++)
{
    nums[i] = rand.Next(30, 40);
}

while (true)
{
    int roundRobin = 0;
    while(tasks.Count < parallelRequests)
    {
        tasks.Add(client.GetAsync("http://localhost:5051/spin?fib=" + nums[roundRobin ++ % parallelRequests]));
    }

    tasks.Remove(await Task.WhenAny(tasks));
}
