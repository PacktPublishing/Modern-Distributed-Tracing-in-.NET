using Grpc.Core;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace server;

public class NotifierService : Nofitier.NofitierBase
{
    private static readonly ActivitySource Source = new("Server.Grpc.Message");
    private readonly TextMapPropagator _propagator;

    public NotifierService(TextMapPropagator propagator)
    {
        _propagator = propagator;
    }

    public override async Task<MessageResponse> SendMessage(Message message, ServerCallContext context)
    {
        try
        {
            return await ProcessMessage(message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            context.Status = new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, ex.Message, ex);
            throw;
        }
    }

    public override async Task SendMessages(
           IAsyncStreamReader<Message> requestStream,
           IServerStreamWriter<MessageResponse> responseStream,
           ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            MessageResponse response = await ProcessMessageWithTracing(request, context.CancellationToken);
            await responseStream.WriteAsync(response);
        }
    }

    private async Task<MessageResponse> ProcessMessageWithTracing(Message message, CancellationToken cancellationToken)
    {
        if (!Source.HasListeners())
        {
            return await ProcessMessage(message, cancellationToken);
        }

        var context = _propagator.Extract(default, message, 
            static (m, k) =>  m.Attributes.TryGetValue(k, out var v) ? new [] { v } : Enumerable.Empty<string>());

        var link = Activity.Current == null ? default : new ActivityLink(Activity.Current.Context);
        // so now we're detaching message from incoming request and making it child of context in the incoming message.
        // we also attach original context as link so we won't lose this information.
        using var act = Source.StartActivity("ProcessMessage", ActivityKind.Consumer, context.ActivityContext, links: new[] { link });

        Baggage.Current = context.Baggage;
        try
        {
            return await ProcessMessage(message, cancellationToken);
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        };
    }

    private static async Task<MessageResponse> ProcessMessage(Message message, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return new MessageResponse { Status = $"ok({ParseIndex(message.Text)})" };
    }

    private static int ParseIndex(string text)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 10 == 0)
        {
            throw new ArgumentException("bad luck", nameof(text));
        }

        var parts = text.Split("-");
        if (parts.Length < 2)
        {
            return 0;
        }

        return int.Parse(parts[1]);
    }
}