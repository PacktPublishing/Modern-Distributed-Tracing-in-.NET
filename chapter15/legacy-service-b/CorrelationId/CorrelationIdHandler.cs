using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyServiceB.CorrelationId
{
    internal class CorrelationIdHandler : DelegatingHandler
    {
        public CorrelationIdHandler(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            requestMessage.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, CorrelationIdMiddleware.CorrelationId);

            return base.SendAsync(requestMessage, cancellationToken);
        }
    }
}
