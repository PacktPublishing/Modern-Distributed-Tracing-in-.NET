using events;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(b => b.AddService("events-sample"))
    .AddSource("Worker")
    .AddJaegerExporter()
    .AddHttpClientInstrumentation()
    .Build()!;

Task task1 = Worker.DoWork(1);
Task task2 = Worker.DoWork(2);

await Task.WhenAll(task1, task2);