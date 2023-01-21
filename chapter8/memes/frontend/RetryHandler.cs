using System.Diagnostics;

namespace frontend;

class RetryHandler : DelegatingHandler
{
    private const int MaxTryCount = 3;
    private readonly static TimeSpan TryTimeout = TimeSpan.FromSeconds(2);
    private readonly static TimeSpan[] delays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10) };
    private readonly ILogger<RetryHandler> _logger;
    public RetryHandler(ILogger<RetryHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        List<Exception>? exceptions = null;
        for (int attempt = 0; attempt < MaxTryCount; attempt++)
        {
            CancellationTokenSource tryToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tryToken.CancelAfter(TryTimeout);
            try
            {
                var response = await LogResponse(await base.SendAsync(request, tryToken.Token));
                if ((int)response.StatusCode != 429 && (int)response.StatusCode < 500)
                {
                    return response;
                }
                _logger.LogWarning("attempt failed; {status} {attempt}", (int)response.StatusCode, attempt);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "attempt failed; {status} {attempt}", e.GetType().Name, attempt);
                exceptions ??= new List<Exception>();
                exceptions.Add(e);
            }

            await Task.Delay(delays[attempt]);
        }

        Activity.Current?.SetStatus(ActivityStatusCode.Error);
        throw new AggregateException("Exhaused all retries", exceptions ?? Enumerable.Empty<Exception>());
    }

    private async Task<HttpResponseMessage> LogResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("got response: {status} {body} {url}", (int)response.StatusCode, 
                await response.Content.ReadAsStringAsync(), 
                response.RequestMessage?.RequestUri);
        }

        return response;
    }
}
