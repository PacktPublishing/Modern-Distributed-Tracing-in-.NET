using Owin;
using System.Net.Http;
using System;
using Microsoft.Extensions.Logging;
using LegacyServiceB.PassThrough;
using LegacyServiceB.CorrelationId;

namespace LegacyServiceB
{
    public enum ContextPropagationMode
    {
        None,
        CorrelationId,
        TraceContextPassThrough
    }

    public class Startup
    {
        private const string ServiceCEndpoint = "http://localhost:5049";
        private readonly ContextPropagationMode _propagationMode;
        private readonly ILoggerFactory _loggerFactory;
        public Startup(ContextPropagationMode propagationMode)
        {
            _propagationMode = propagationMode;
            _loggerFactory = LoggingHelpers.CreateFactory();
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            ConfigurePropagationMiddleware(appBuilder);
            appBuilder.Use(typeof(LoggingMiddleware), _loggerFactory);
            appBuilder.Use(typeof(RequestProcessingMiddleware), CreateHttpClient(ServiceCEndpoint));
        }

        private HttpClient CreateHttpClient(string BaseAddress)
        {
            var client = new HttpClientHandler();

            var loggingHandler = new LoggingHandler(client, _loggerFactory);
            var propagationHandler = CreatePropagationHandler(loggingHandler);
            return new HttpClient(propagationHandler)
            {
                BaseAddress = new Uri(BaseAddress)
            };
        }

        private void ConfigurePropagationMiddleware(IAppBuilder appBuilder)
        {
            switch (_propagationMode)
            {
                case ContextPropagationMode.TraceContextPassThrough:
                    appBuilder.Use(typeof(PassThroughMiddleware), _loggerFactory);
                    break;
                case ContextPropagationMode.CorrelationId:
                    appBuilder.Use(typeof(CorrelationIdMiddleware), _loggerFactory);
                    break;
            }
        }

        private DelegatingHandler CreatePropagationHandler(DelegatingHandler inner)
        {
            switch (_propagationMode)
            {
                case ContextPropagationMode.TraceContextPassThrough:
                    return new PassThroughHandler(inner);
                case ContextPropagationMode.CorrelationId:
                    return new CorrelationIdHandler(inner);
            }

            return inner;
        }
    }
}