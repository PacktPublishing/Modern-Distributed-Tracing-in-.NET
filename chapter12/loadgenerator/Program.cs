using loadgenerator;
using System.CommandLine;
using System.Threading.RateLimiting;

var command = new RootCommand("Load generator");
var countOption = new Option<long>("--count", () => -1, "Total request count, set -1 to run endlessly");
var parallelRequestsOption = new Option<int>("--rate", () => 50, "Request rate per second");
command.AddGlobalOption(parallelRequestsOption);
command.AddGlobalOption(countOption);
command.SetHandler((parallelRequests, count) => Loop("http://localhost:5051", parallelRequests, count), parallelRequestsOption, countOption);

await command.InvokeAsync(args);

static async Task Loop(string endpoint, int parallelRequests, long count)
{
    var tasks = new HashSet<Task>();
    var helper = new Helper(endpoint, TimeSpan.FromSeconds(1));
    var options = new TokenBucketRateLimiterOptions()
    {
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        TokensPerPeriod = parallelRequests,
        TokenLimit = parallelRequests,
    };

    var rateLimiter = new TokenBucketRateLimiter(options);
    if (count < 0) count = long.MaxValue;
    for (long i = 0; i < count;)
    {
        using var lease = rateLimiter.AttemptAcquire();
        if (lease.IsAcquired)
        {
            await helper.GetOrCreate();
        }
        else if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await Task.Delay(retryAfter);
        }
    }
}
