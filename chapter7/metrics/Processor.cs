using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace metrics;

internal class Processor : IDisposable
{
    private static readonly ActivitySource Source = new(nameof(Processor));

    private readonly ObservableUpDownCounter<int> _queueLengthCounter;
    private readonly ObservableGauge<long> _sequenceNumberGauge;
    private readonly Histogram<double> _processorLagHistogram;
    private readonly Histogram<double> _processingDurationHistogram;

    private readonly Meter _meter = new("queue.processor");
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<WorkItem> _queue;
    private readonly KeyValuePair<string, object?> _queueNameTag;
    private readonly string _queueName;
    private readonly Random _random = new();
    private Task? _loop = null;
    private long _seqNo;

    public Processor(ConcurrentQueue<WorkItem> queue, string queueName)
    {
        _queue = queue;
        _queueName = queueName;
        _queueNameTag = new KeyValuePair<string, object?>("queue", queueName);

        _meter = new Meter("queue.processor");
        _queueLengthCounter = _meter.CreateObservableUpDownCounter("queue.length", () => new Measurement<int>(_queue.Count, _queueNameTag), "{items}", "Queue length");
        _sequenceNumberGauge = _meter.CreateObservableGauge("processor.last_sequence_number", () => new Measurement<long>(_seqNo, _queueNameTag), description: "Sequence number of the last dequeued item");
        _processorLagHistogram = _meter.CreateHistogram<double>("processor.lag", "ms", "Time items spend in queue");
        _processingDurationHistogram = _meter.CreateHistogram<double>("processor.processing.duration", "ms", "Item processing duration");
    }

    public void Start()
    {
        _loop = Task.Run(async () =>
        {
            while (!(_cts.IsCancellationRequested && _queue.IsEmpty))
            {
                if (_queue.TryDequeue(out var item))
                {
                    await ProcessInstrumented(item);
                }
                else
                {
                    await Task.Delay(10);
                }
            }

            Console.WriteLine($"Processor for '{_queueName}' is shutting down.");
        });
    }

    private async Task<ProcessingStatus> ProcessInstrumented(WorkItem item)
    {
        _seqNo = item.SequenceNumber;

        //Console.WriteLine($"Processing work item {item.SequenceNumber} from queue '{_queueName}'");

        using var activity = Source.StartActivity(ActivityKind.Consumer, item.Context);
        if (activity?.IsAllDataRequested == true)
        {
            activity
                .SetTag("work_item.sequence_number", item.SequenceNumber)
                .SetTag("work_item.queue_name", _queueName);
        }

        if (_processorLagHistogram.Enabled)
            _processorLagHistogram.Record((DateTimeOffset.UtcNow - item.CreatedTimeUtc).TotalMilliseconds, _queueNameTag);

        Stopwatch? duration = _processingDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        var status = await Process(item);
        if (duration != null)
            _processingDurationHistogram.Record(duration.Elapsed.TotalMilliseconds, _queueNameTag, new KeyValuePair<string, object?>("status", StatusToString(status)));

        return status;
    }

    private async Task<ProcessingStatus> Process(WorkItem item)
    {
        await Task.Delay(GetDelay());

        if (item.SequenceNumber % 7 == 0)
        {
            return ProcessingStatus.Failed;
        }
        else if (item.SequenceNumber % 11 == 0)
        {
            return ProcessingStatus.Cancelled;
        }

        return ProcessingStatus.Ok;
    }

    private static string StatusToString(ProcessingStatus status) => status switch {
        ProcessingStatus.Ok => "Ok",
        ProcessingStatus.Cancelled => "Cancelled",
        _ => "Failed"
    };

    private int GetDelay()
    {
        return _random.Next(10, _queueName switch
        {
            "add" => 100,
            "remove" => 80,
            _ => 120
        });
    }

    public Task Stop()
    {
        _cts.Cancel();
        return _loop ?? Task.CompletedTask;
    }

    public void Dispose()
    {
        Stop().GetAwaiter().GetResult();
        _meter.Dispose();
    }

    enum ProcessingStatus
    {
        Ok,
        Cancelled,
        Failed
    }
}
