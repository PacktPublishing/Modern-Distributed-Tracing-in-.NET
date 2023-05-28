using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using System;
using System.Threading.Tasks;

namespace LegacyServiceB
{
    internal class LoggingMiddleware : OwinMiddleware
    {
        private readonly ILogger<LoggingMiddleware> _logger;
        public LoggingMiddleware(OwinMiddleware next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
            : base(next)
        {
            _logger = loggerFactory.CreateLogger<LoggingMiddleware>();
        }

        public async override Task Invoke(IOwinContext context)
        {
            _logger.LogInformation("Request started. {method} {path}", context.Request.Method, context.Request.Path.Value);
            try
            {
                await Next.Invoke(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during request");
            }
            finally
            {
                _logger.LogInformation("Request complete. {method} {path}, {status}", context.Request.Method, context.Request.Path.Value, context.Response.StatusCode);
            }
        }
    }
}
