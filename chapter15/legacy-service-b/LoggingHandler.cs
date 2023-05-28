using Microsoft.Extensions.Logging;
using Microsoft.Owin.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyServiceB
{
    internal class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        public LoggingHandler(HttpMessageHandler inner, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base(inner)
        {
            _logger = loggerFactory.CreateLogger<LoggingHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Request started. {method} {url}", requestMessage.Method, requestMessage.RequestUri.AbsoluteUri);
            try
            {
                var response = await base.SendAsync(requestMessage, cancellationToken);
                _logger.LogInformation("Request complete. {method} {url}, {status}", requestMessage.Method, requestMessage.RequestUri.AbsoluteUri, response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed. {method} {url}", requestMessage.Method, requestMessage.RequestUri.AbsoluteUri);
                throw;
            }           
        }
    }
}
