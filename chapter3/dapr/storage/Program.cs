using storage;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var daprUrl = builder.Configuration.GetSection("Dapr")?["Endpoint"] ?? "http://localhost:3500";
builder.Services.AddHttpClient("daprBindings", httpClient =>
{
    httpClient.BaseAddress = new Uri(daprUrl + "/v1.0/bindings/");
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddControllers();

ConfigureCloudStorage(builder);
ConfigureTelemetry(builder);

var app = builder.Build();

app.MapControllers();

app.Run();

static void ConfigureCloudStorage(WebApplicationBuilder builder)
{
    builder.Services.Configure<CloudStorageOptions>(builder.Configuration.GetSection("CloudStorage"));
    builder.Services.AddSingleton<DaprStorageBinding>();
    builder.Services.AddSingleton<IStorageService>(resolver =>
    {
        var storageOptions = resolver.GetRequiredService<IOptions<CloudStorageOptions>>();
        var daprBinding = resolver.GetRequiredService<DaprStorageBinding>();

        switch (storageOptions.Value.Type)
        {
            case CloudStorageOptions.StorageType.AzureBlob:
                return new AzureBlobStorage(daprBinding);
            case CloudStorageOptions.StorageType.AwsS3:
                return new AwsS3Storage(daprBinding);
            case CloudStorageOptions.StorageType.Local:
            default:
                return new LocalStorage(daprBinding);
        }
    });
}

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var collectorEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
    if (collectorEndpoint == null) 
    {
        return;
    }

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
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
