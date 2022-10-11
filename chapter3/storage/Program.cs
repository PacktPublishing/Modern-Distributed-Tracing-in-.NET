using storage;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var redisConnection = ConfigureRedis(builder);
ConfigureCloudStorage(builder);
ConfigureTelemetry(builder, redisConnection);

var app = builder.Build();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void ConfigureCloudStorage(WebApplicationBuilder builder)
{
    builder.Services.Configure<CloudStorageOptions>(builder.Configuration.GetSection("CloudStorage"));
    builder.Services.AddSingleton<IStorageService>(resolver =>
    {
        var storageOptions = resolver.GetRequiredService<IOptions<CloudStorageOptions>>();
        switch (storageOptions.Value.Type)
        {
            case CloudStorageOptions.StorageType.AzureBlob:
                return new AzureBlobStorage(storageOptions);
            case CloudStorageOptions.StorageType.S3:
                return new S3Storage(storageOptions);
            case CloudStorageOptions.StorageType.Redis:
            default:
                var redisConnection = resolver.GetRequiredService<IConnectionMultiplexer>();
                return new RedisStorage(redisConnection.GetDatabase());
        }
    });
}

static IConnectionMultiplexer ConfigureRedis(WebApplicationBuilder builder)
{
    var redisConnectionString = builder.Configuration.GetSection("Redis")["connectionString"];
    IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton(redisConnection);

    builder.Services.AddOptions<RedisCacheOptions>().Configure<IConnectionMultiplexer>((options, redisConnection) =>
    {
        options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection);
        if (options.ConfigurationOptions == null) 
        {
            options.ConfigurationOptions = new ConfigurationOptions();
        }
        options.ConfigurationOptions.ResolveDns = true;
    });

    builder.Services.AddSingleton<IDistributedCache>(resolver =>
    {
        var storageOptions = resolver.GetRequiredService<IOptions<CloudStorageOptions>>();
        var redisOptions = resolver.GetRequiredService<IOptions<RedisCacheOptions>>();
        if (storageOptions.Value.Type == CloudStorageOptions.StorageType.Redis)
        {
            // does not make sense to use cache in front of redis
            return new NoopCache();
        }
        else
        {
            return new RedisCache(redisOptions);
        }
    });

    // we'll need it to configure OTel instrumetnation
    return redisConnection;
}

static void ConfigureTelemetry(WebApplicationBuilder builder, IConnectionMultiplexer redisConnection)
{
    var collectorEndpoint = builder.Configuration.GetSection("OtelCollector")?.GetValue<string>("Endpoint");
    if (collectorEndpoint == null)
    {
        return;
    }

    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/traces");
        })
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        // enable redis instrumentation - it needs connection instance
        .AddRedisInstrumentation(redisConnection, options => options.SetVerboseDatabaseStatements = true)
        // enable Azure SDK instrumentation
        .AddSource("Azure.*")
        // enable AWS instrumentation
        .AddAWSInstrumentation();
    });

    builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
        meterProviderBuilder.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/metrics");
        })
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());

    builder.Logging.AddOpenTelemetry(options =>
        options.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/logs");
        }));
}
