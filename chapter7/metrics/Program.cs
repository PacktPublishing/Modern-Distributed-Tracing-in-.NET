using metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(nameof(Processor))
    .AddSource(nameof(Producer))
    .AddOtlpExporter()
    .Build()!;
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("queue.*")
    .AddConsoleExporter()
    .AddOtlpExporter()
    .Build()!;

long seqNo = 0;
var produceToken = new CancellationTokenSource();
var queueNames = new[] { "add", "update", "remove" };
var queues = new ConcurrentQueue<WorkItem>[queueNames.Length];
var producers = new Producer[queueNames.Length];
var processors = new Processor[queueNames.Length];
var produceTasks = new Task[queueNames.Length];
for (int i = 0; i < queueNames.Length; i++)
{
    queues[i] = new ();
    producers[i] = new (queues[i], queueNames[i]);
    processors[i] = new(queues[i], queueNames[i]);
    processors[i].Start();

    Producer producer = producers[i];
    produceTasks[i] = Task.Run(async () =>
    {
        while (!produceToken.IsCancellationRequested)
        {
            producer.Enqueue(Interlocked.Increment(ref seqNo));
            await Task.Delay(50, produceToken.Token).ContinueWith(_ => { });
        }
    });
}

Console.CancelKeyPress += (_, args) =>
{
    produceToken.Cancel();
    args.Cancel = true;
};

await Task.WhenAll(produceTasks);
Console.WriteLine("Waiting for processors to process remaining items...");
await Task.WhenAll(processors.Select(c => c.Stop()));

