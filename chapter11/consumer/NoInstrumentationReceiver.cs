using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace consumer;

public class NoInstrumentationReceiver : BackgroundService
{
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BackoffTimeout = TimeSpan.FromSeconds(1);
    private readonly ILogger<BatchReceiver> _logger;
    private readonly QueueClient _queue;

    public NoInstrumentationReceiver(QueueClient queueClient, ILogger<BatchReceiver> logger)
    {
        _queue = queueClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var response = await _queue.ReceiveMessagesAsync(1, ProcessingTimeout, token);
                QueueMessage[] messages = response.Value;
                if (messages.Length == 0)
                {
                    await Task.Delay(1000, token);
                    continue;
                }

                QueueMessage message = messages[0];
                try
                {
                    await ProcessMessage(message);
                    await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, token);
                }
                catch (Exception)
                {
                    await _queue.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: BackoffTimeout, cancellationToken: token);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "processing failed");
            } 
        }
    }

    private async Task ProcessMessage(QueueMessage message)
    {
        Message msg = message.Body.ToObjectFromJson<Message>();
        _logger.LogInformation("received message {message}", msg.Text);
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 3 == 0)
        {
            throw new Exception("bad luck");
        }
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 11 == 0)
        {
            await Task.Delay(500);
        }
        await Task.Delay(100);
    }
}
