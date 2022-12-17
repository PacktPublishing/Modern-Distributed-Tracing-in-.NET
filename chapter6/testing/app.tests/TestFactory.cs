using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace app.tests;

public class TestFactory : WebApplicationFactory<Program>
{
    public static readonly TestActivityProcessor Processor = new ();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(s =>
        {
            s.ConfigureOpenTelemetryTracerProvider((_, traceProviderBuilder) =>
                traceProviderBuilder.AddProcessor(Processor)
                .AddSource("Test"));


            var cache = s.Single(s => s.ServiceType== typeof(IMemoryCache));
            s.Remove(cache);
            s.AddSingleton<IMemoryCache>(Cache);

            var storage = s.Single(s => s.ServiceType == typeof(IStorageService));
            s.Remove(storage);
            s.AddSingleton<IStorageService>(Storage);
        });
    }

    public readonly MemoryCache Cache = new(new MemoryCacheOptions());
    public readonly LocalStorage Storage = new();

    public void Reset()
    {
        Cache.Clear();
        Storage.Clear();
    }
}
