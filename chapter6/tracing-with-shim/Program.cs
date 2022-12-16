using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using tracing_with_otel_api;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService("otel-api-sample"))
    .AddSource("Worker")
    .AddJaegerExporter()
    .Build()!;


await Worker.DoWork(new WorkItem(1, "add_user"));