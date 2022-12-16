using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace app.tests;

public class TracingTestsWebAppFactory
    : WebApplicationFactory<Program>
{
    public static readonly TestActivityProcessor Processor = new ();
    public readonly MemoryCache Cache = new (new MemoryCacheOptions());
    public readonly LocalStorage Storage = new ();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            Reset();
            services.ConfigureOpenTelemetryTracerProvider((_, traceProviderBuilder) =>
                traceProviderBuilder.AddProcessor(Processor)
                .AddSource("Test"));

            var cache = services.Single(s => s.ServiceType== typeof(IMemoryCache));
            services.Remove(cache);
            services.AddSingleton<IMemoryCache>(Cache);

            var storage = services.Single(s => s.ServiceType == typeof(IStorageService));
            services.Remove(storage);
            services.AddSingleton<IStorageService>(Storage);
        });
    }

    private void Reset()
    {
        Cache.Clear();
        Storage.Clear();
    }
}
