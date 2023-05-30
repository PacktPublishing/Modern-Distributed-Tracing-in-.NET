using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyServiceB.PassThrough
{
    internal class PassThroughMiddleware : OwinMiddleware
    {
        private static readonly Dictionary<string, object> EmptyContext = new Dictionary<string, object>();
        private static readonly AsyncLocal<IDictionary<string, object>> _currentContext = new AsyncLocal<IDictionary<string, object>>();
        private readonly ILogger<PassThroughMiddleware> _logger;

        public PassThroughMiddleware(OwinMiddleware next, ILoggerFactory loggerFactory)
            : base(next)
        {
            _logger = loggerFactory.CreateLogger<PassThroughMiddleware>();
        }

        public static IDictionary<string, object> CurrentContext => _currentContext.Value;
        public override async Task Invoke(IOwinContext context)
        {
            var tc = EmptyContext;
            if (context.Request.Headers.TryGetValue("traceparent", out var traceparent))
            {
                tc = new Dictionary<string, object> {{ "traceparent", traceparent[0] }};

                if (context.Request.Headers.TryGetValue("tracestate", out var tracestates))
                    tc.Add("tracestate", string.Join(",", tracestates));

                if (context.Request.Headers.TryGetValue("baggage", out var baggages))
                    tc.Add("baggage", string.Join(",", baggages));


            }
            _currentContext.Value = tc;

            using (var scope = _logger.BeginScope(tc))
            {
                await Next.Invoke(context);
            }
        }
    }
}
