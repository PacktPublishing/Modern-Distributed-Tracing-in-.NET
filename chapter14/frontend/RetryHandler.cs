using Memes.OpenTelemetry.Common;

namespace frontend;

class RetryHandler : DelegatingHandler
{
    private const int MaxTryCount = 3;
    private readonly static TimeSpan TryTimeout = TimeSpan.FromSeconds(2);
    private readonly static TimeSpan[] delays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        List<Exception>? exceptions = null;
        for (int i = 0; i < MaxTryCount; i++)
        {
            CancellationTokenSource tryToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tryToken.CancelAfter(TryTimeout);
            try
            {
                request.Options.Set(new HttpRequestOptionsKey<int>(SemanticConventions.HttpResendCountKey), i);
                var response = await base.SendAsync(request, tryToken.Token);

                if ((int)response.StatusCode != 429 && (int)response.StatusCode < 500)
                {
                    return response;
                }
            }
            catch (Exception e)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(e);
            }

            await Task.Delay(delays[i], cancellationToken);
        }

        throw new AggregateException("Exhaused all retries", exceptions ?? Enumerable.Empty<Exception>());
    }
}
