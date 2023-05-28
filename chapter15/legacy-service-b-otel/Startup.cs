using Owin;
using System.Net.Http;
using System;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace LegacyServiceBOTel
{
    public class Startup
    {
        private const string ServiceCEndpoint = "http://localhost:5049";

        public void Configuration(IAppBuilder appBuilder)
        {
            Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("b"))
                .AddOwinInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317"))
                .Build();

            appBuilder.UseOpenTelemetry();

            var loggerFactory = LoggingHelpers.CreateFactory();
            appBuilder.Use(typeof(LoggingMiddleware), loggerFactory);
            appBuilder.Use(typeof(RequestProcessingMiddleware), CreateHttpClient(ServiceCEndpoint, loggerFactory));
        }

        private static HttpClient CreateHttpClient(string BaseAddress, ILoggerFactory loggerFactory)
        {
            var client = new HttpClientHandler();
            var loggingHandler = new LoggingHandler(client, loggerFactory);
            return new HttpClient(loggingHandler)
            {
                BaseAddress = new Uri(BaseAddress)
            };
        }
    }
}