using Azure.Storage.Queues;
using consumer;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;

var serviceAttributes = new[] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName) };

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services
            .Configure<QueueOptions>(context.Configuration.GetRequiredSection("Queue"))
            .AddSingleton(s =>
                {
                    var queueOptions = s.GetRequiredService<IOptions<QueueOptions>>().Value;
                    return new QueueServiceClient(queueOptions.ConnectionString)
                        .GetQueueClient(queueOptions.Name);
                })
            .AddSingleton<TextMapPropagator, TraceContextPropagator>()
            .AddHostedService<QueueSizeReporter>()
            //.AddHostedService<SingleReceiver>();
            .AddHostedService<BatchReceiver>();

        services
            .AddOpenTelemetry()
            .WithTracing(b => b
                .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
                .AddSource("Azure.Storage.*")
                .AddSource("Queue.*")
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(b => b
                .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
                .AddMeter("Queue.Size")
                .AddMeter("Queue.Receive")
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter());

    })
    .ConfigureLogging(builder => builder
        .AddOpenTelemetry(b =>
        {
            b.ParseStateValues = true;
            b.AddOtlpExporter();
        }))
    .Build();

CreateQueue(host.Services);

host.Run();

static void CreateQueue(IServiceProvider services)
{
    var queueClient = services.GetRequiredService<QueueClient>();
    queueClient.CreateIfNotExists();
}
