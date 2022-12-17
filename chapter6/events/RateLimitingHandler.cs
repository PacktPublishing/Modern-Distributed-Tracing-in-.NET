using System.Diagnostics;
using System.Net;
using System.Threading.RateLimiting;

namespace events;

class RateLimitingHandler : DelegatingHandler
{
    private static readonly TokenBucketRateLimiterOptions Options = new TokenBucketRateLimiterOptions()
    {
        ReplenishmentPeriod = TimeSpan.FromSeconds(5),
        TokensPerPeriod = 1,
        TokenLimit = 1,
    };

    private readonly TokenBucketRateLimiter _rateLimiter = new (Options);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var lease = _rateLimiter.AttemptAcquire();
        if (lease.IsAcquired)
        {
            return await base.SendAsync(req, ct);
        }

        var res = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var work = Activity.Current;
            if (work?.IsAllDataRequested == true)
            {
                var tags = new ActivityTagsCollection();
                tags.Add("exception.type", "rate_is_limited");
                tags.Add("retry_after_ms", retryAfter.TotalMilliseconds);

                work?.AddEvent(new ActivityEvent("exception", tags: tags));
            }
            res.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString());
        }
        return res;
    }

    public RateLimitingHandler() : base(new SocketsHttpHandler())
    {
    }
}