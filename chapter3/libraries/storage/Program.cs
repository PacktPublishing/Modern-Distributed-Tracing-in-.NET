using storage;
using OpenTelemetry.Trace;
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
            case CloudStorageOptions.StorageType.AwsS3:
                return new S3Storage(storageOptions);
            case CloudStorageOptions.StorageType.Local:
            default:
                var redisConnection = resolver.GetRequiredService<IConnectionMultiplexer>();
                return new RedisStorage(redisConnection.GetDatabase());
        }
    });
}

static IConnectionMultiplexer ConfigureRedis(WebApplicationBuilder builder)
{
    var redisConnectionString = builder.Configuration.GetSection("Redis")["connectionString"];
    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
    configurationOptions.ConnectRetry = 10;
    configurationOptions.ConnectTimeout = 30000;

    IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(configurationOptions);
    builder.Services.AddSingleton(redisConnection);

    builder.Services.AddOptions<RedisCacheOptions>().Configure<IConnectionMultiplexer>((options, redisConnection) =>
    {
        options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection);
        options.ConfigurationOptions = configurationOptions;
    });

    builder.Services.AddSingleton<IDistributedCache>(resolver =>
    {
        var storageOptions = resolver.GetRequiredService<IOptions<CloudStorageOptions>>();
        var redisOptions = resolver.GetRequiredService<IOptions<RedisCacheOptions>>();
        if (storageOptions.Value.Type == CloudStorageOptions.StorageType.Local)
        {
            // does not make sense to use cache in front of redis
            return new NoopCache();
        }
        else
        {
            return new RedisCache(redisOptions);
        }
    });

    // we'll need it to configure OTel instrumentation
    return redisConnection;
}

static void ConfigureTelemetry(WebApplicationBuilder builder, IConnectionMultiplexer redisConnection)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
            // enable redis instrumentation - it needs connection instance
            .AddRedisInstrumentation(redisConnection, o => o.SetVerboseDatabaseStatements = true)
            // enable AWS instrumentation
            .AddAWSInstrumentation(o => o.SuppressDownstreamInstrumentation = false)
            // enable Azure SDK instrumentation
            .AddSource("Azure.Storage.*")
            .AddOtlpExporter()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddOtlpExporter()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation());

    builder.Logging.AddOpenTelemetry(options =>
        options.AddOtlpExporter());
}
