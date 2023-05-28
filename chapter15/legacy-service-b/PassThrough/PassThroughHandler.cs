using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyServiceB.PassThrough
{
    internal class PassThroughHandler : DelegatingHandler
    {
        public PassThroughHandler(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            foreach (var kvp in PassThroughMiddleware.CurrentContext)
                request.Headers.Add(kvp.Key, kvp.Value?.ToString());

            return base.SendAsync(request, token);
        }
    }
}
