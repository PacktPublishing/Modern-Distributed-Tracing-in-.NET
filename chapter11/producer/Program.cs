using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Context.Propagation;
using producer;
using OpenTelemetry.Resources;

var serviceAttributes = new[] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName) };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .Configure<QueueOptions>(builder.Configuration.GetRequiredSection("Queue"))
    .AddSingleton(s =>
    {
        var queueOptions = s.GetRequiredService<IOptions<QueueOptions>>().Value;
        return new QueueServiceClient(queueOptions.ConnectionString)
            .GetQueueClient(queueOptions.Name);
    })
    .AddSingleton<TextMapPropagator, TraceContextPropagator>();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
        //.AddSource("Azure.Storage.*")
        .AddSource("Queue.*")
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
        .AddMeter("Queue.Publish")
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(b => {
    b.ParseStateValues = true;
    b.AddOtlpExporter();
});

var app = builder.Build();

CreateQueue(app.Services);
app.MapControllers();

app.Run();

static void CreateQueue(IServiceProvider services)
{
    var queueClient = services.GetRequiredService<QueueClient>();
    queueClient.CreateIfNotExists();
}
