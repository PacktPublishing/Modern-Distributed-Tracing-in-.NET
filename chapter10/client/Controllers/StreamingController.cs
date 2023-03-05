using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace client.Controllers;

[ApiController]
[Route("[controller]")]
public class StreamingController : ControllerBase
{
    private static readonly ActivitySource Source = new ("Client.Grpc.Message");
    private readonly Nofitier.NofitierClient _client;
    private readonly ILogger<StreamingController> _logger;
    private readonly TextMapPropagator _propagator;
    public record ClientMessages(ClientMessage[] messages);

    public StreamingController(Nofitier.NofitierClient client, TextMapPropagator propagator, ILogger<StreamingController> logger)
    {
        _client = client;
        _logger = logger;
        _propagator = propagator;
    }

    [HttpGet("{text}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public IAsyncEnumerable<MessageResponse> Get(string text, [FromQuery] int count, CancellationToken cancellationToken)
    {
        var call = _client.SendMessages(cancellationToken: cancellationToken);

        Task send = Task.Run(async () =>
        {
            for (int i = 0; i < count && !cancellationToken.IsCancellationRequested; i++)
            {
                var grpcMessage = new Message() { Text = $"{text} - {i}" };
                grpcMessage.Attributes.Add("delay", (i * 100).ToString());
                await SendMessageWithTracing(call.RequestStream, grpcMessage, cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }

            await call.RequestStream.CompleteAsync();
        }, cancellationToken);

        return StreamResponse(call);
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public IAsyncEnumerable<MessageResponse> Post([FromBody] ClientMessages clientMessages, CancellationToken cancellationToken)
    {
        var call = _client.SendMessages(cancellationToken: cancellationToken);
        Task send = Task.Run(async () =>
        {
            foreach (var clientMessage in clientMessages.messages)
            {
                await SendMessageWithTracing(call.RequestStream, clientMessage.ToGrpcMessage(), cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }

            await call.RequestStream.CompleteAsync();
        }, cancellationToken);

        return StreamResponse(call);
    }

    private Task SendMessageWithTracing(IClientStreamWriter<Message> requestStream, Message message, CancellationToken cancellationToken)
    {
        if (!Source.HasListeners())
        {
            return requestStream.WriteAsync(message);
        }

        // since we're going to modify Activity.Current, we should do it inside async call
        // limiting the scope of this change to what happens within this task and it's children.
        return Task.Run(async () =>
        {
            IEnumerable<ActivityLink>? links = null;
            if (Activity.Current != null)
            {
                // so now we're detaching message from incoming request, starting a new trace for each message. 
                // we also attach original context as link
                links = new[] { new ActivityLink(Activity.Current.Context) };
                Activity.Current = null;
            }

            using var act = Source.StartActivity("SendMessage", ActivityKind.Producer, default(ActivityContext), links: links);
            if (act != null)
                _propagator.Inject(new PropagationContext(act.Context, Baggage.Current), message, static (m, k, v) => m.Attributes.Add(k, v));

            try
            {
                await requestStream.WriteAsync(message);
            }
            catch (Exception ex)
            {
                act?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }, cancellationToken);
    }

    private async IAsyncEnumerable<MessageResponse> StreamResponse(AsyncDuplexStreamingCall<Message, MessageResponse> call)
    {
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            _logger.LogInformation("got {response}", response);
            yield return response;
        }

        call.Dispose();
    }
}
