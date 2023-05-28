using Microsoft.Owin;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LegacyServiceBOTel
{
    internal class RequestProcessingMiddleware : OwinMiddleware
    {
        private readonly HttpClient _serviceC;

        public RequestProcessingMiddleware(OwinMiddleware next, HttpClient serviceC)
            : base(next)
        {
            _serviceC = serviceC;
        }

        public async override Task Invoke(IOwinContext ctx)
        {
            try
            {
                var response = await _serviceC.GetStringAsync(ctx.Request.Path.Value);

                ctx.Response.Write(response);
                ctx.Response.StatusCode = 200;
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = 500;
                throw;
            }
        }
    }
}
