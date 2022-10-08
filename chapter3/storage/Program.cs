using storage;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using StackExchange.Redis;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string redisConnectionString = builder.Configuration.GetSection("Redis")["connectionString"];
IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection);
    options.ConfigurationOptions.ResolveDns = true;
    options.ConfigurationOptions.ConnectRetry = 10;
    options.ConfigurationOptions.AbortOnConnectFail = false;
    options.ConfigurationOptions.ConnectTimeout = 30000;
});
builder.Services.AddSingleton(redisConnection.GetDatabase());

//builder.Services.Configure<S3Options>(builder.Configuration.GetSection("S3"));
//builder.Services.AddSingleton<IStorageService, S3Storage>();


builder.Services.Configure<AzureBlobStorageOptions>(builder.Configuration.GetSection("AzureBlob"));
builder.Services.AddSingleton<IStorageService, AzureBlobStorage>();


ConfigureTelemetry(builder, redisConnection);

var app = builder.Build();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder, IConnectionMultiplexer redisConnection)
{
    var collectorEndpoint = builder.Configuration.GetSection("OtelCollector")?.GetValue<string>("Endpoint");
    if (collectorEndpoint == null)
    {
        return;
    }

    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        tracerProviderBuilder.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(collectorEndpoint + "/v1/traces");
        })
        .AddSource("Azure.*")
        .AddRedisInstrumentation(redisConnection)
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddAWSInstrumentation());

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