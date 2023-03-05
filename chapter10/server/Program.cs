using server;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var contextPropagator = new CompositeTextMapPropagator(new TextMapPropagator [] { new TraceContextPropagator(), new BaggagePropagator()});
Sdk.SetDefaultTextMapPropagator(contextPropagator);

builder.Services.AddControllers();

builder.Services
    .AddSingleton<TextMapPropagator>(contextPropagator)
    .AddGrpc(o =>
    {
        o.Interceptors.Add<GrpcTracingInterceptor>();
        o.Interceptors.Add<GrpcMetricsInterceptor>();
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("Server.Grpc")
        // enables per-message tracing
        .AddSource("Server.Grpc.Message")
        // enables server auto-instrumentation for HTTP and gRPC
        //.AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddMeter("Server.Grpc")
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(b => {
        b.ParseStateValues = true;
        b.AddOtlpExporter();
    });


var app = builder.Build();

app.MapGrpcService<NotifierService>();
app.Run();
