using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyServiceB.CorrelationId
{
    internal class CorrelationIdMiddleware : OwinMiddleware
    {
        public const string CorrelationIdHeaderName = "correlation-id";

        private static AsyncLocal<string> correlationId = new AsyncLocal<string>();

        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(OwinMiddleware next, ILoggerFactory loggerFactory)
            : base(next)
        {
            _logger = loggerFactory.CreateLogger<CorrelationIdMiddleware>();
        }

        public static string CorrelationId
        {
            get { return correlationId.Value; }
            set { correlationId.Value = value; }
        }

        public override async Task Invoke(IOwinContext context)
        {
            correlationId.Value = GetCorrelationId(context);
            using (var scope = _logger.BeginScope("{correlationId}", correlationId.Value))
            {
                await Next.Invoke(context);
            }
        }


        private static string GetCorrelationId(IOwinContext context)
        {
            if (context.Request.Headers.TryGetValue("correlation-id", out var correlationIds))
            {
                return correlationIds[0];
            }

            return Guid.NewGuid().ToString();
        }
    }
}
