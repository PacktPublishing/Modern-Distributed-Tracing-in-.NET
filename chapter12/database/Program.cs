using database;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<MongoDbSettings>(builder.Configuration.GetSection("mongo"))
    .AddSingleton<DatabaseService>();

builder.Services.AddStackExchangeRedisCache(o =>
    {
        var redisConnectionString = builder.Configuration.GetSection("redis")["ConnectionString"];
        var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
        configurationOptions.ConnectRetry = 10;
        configurationOptions.ConnectTimeout = 1000;
        configurationOptions.SyncTimeout = 1000;

        o.ConfigurationOptions = configurationOptions;
    })
    .AddSingleton<CacheService>();

builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        //.SetSampler(new TraceIdRatioBasedSampler(0.01))
        .AddAspNetCoreInstrumentation()
        .AddSource("MongoDb")
        .AddSource("Redis")
        .AddSource("Records")
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b.AddAspNetCoreInstrumentation()
        .AddMeter("MongoDb")
        .AddMeter("Redis")
        .AddHttpClientInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.MapControllers();

app.Run();
