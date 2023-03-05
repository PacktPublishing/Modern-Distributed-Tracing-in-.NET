using client;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

CompositeTextMapPropagator contextPropagator = new (new TextMapPropagator[] { new TraceContextPropagator(), new BaggagePropagator() });
Sdk.SetDefaultTextMapPropagator(contextPropagator);

builder.Services.AddControllers();

var serverEndpoint = new Uri(builder.Configuration.GetSection("Server").GetValue<string>("Endpoint") ?? "https://localhost:7070");

builder.Services
    .AddSingleton<TextMapPropagator>(contextPropagator)
    .AddGrpcClient<Nofitier.NofitierClient>(o =>
    {
        o.Address = serverEndpoint;
        o.ChannelOptionsActions.Add(ConfigureChannel);
    })
    .AddInterceptor(() => new GrpcTracingInterceptor(serverEndpoint, contextPropagator))
    .AddInterceptor(() => new GrpcMetricsInterceptor(serverEndpoint));

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("Client.Grpc")
        // enable per-message tracing
        .AddSource("Client.Grpc.Message")
        
        // enables gRPC auto instrumentation (both are needed)
        //.AddGrpcClientInstrumentation()
        //.AddHttpClientInstrumentation()

        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddMeter("Client.Grpc")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(b => {
    b.ParseStateValues = true;
    b.AddOtlpExporter();
});

var app = builder.Build();
app.MapControllers();
app.Run();

static void ConfigureChannel(GrpcChannelOptions channelOptions)
{
    // dangerous! for demo purposes only.
    channelOptions.HttpHandler = new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };


    // when retry policy is applied here, retries are done by the underlying HTTP client, so that
    // grpc span created in GrpcTracingInterceptor would be a parent to the tries.
    // and GrpcClientInstrumentationInterceptor would trace logical calls instead.
    // commenting it out to demonstrate context propagation.
    var retryPolicy = new RetryPolicy
    {
        MaxAttempts = 5,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(5),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable, Grpc.Core.StatusCode.Internal, Grpc.Core.StatusCode.Unknown }
    };

    channelOptions.ServiceConfig = new ServiceConfig()
    {
        MethodConfigs = { new MethodConfig() {
            Names = { MethodName.Default },
            RetryPolicy = retryPolicy }
        }
    };
}