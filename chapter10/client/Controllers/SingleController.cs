using Microsoft.AspNetCore.Mvc;

namespace client.Controllers;

[ApiController]
[Route("[controller]")]
public class SingleController : ControllerBase
{
    private readonly Nofitier.NofitierClient _client;
    private readonly ILogger<StreamingController> _logger;

    public SingleController(Nofitier.NofitierClient client, ILogger<StreamingController> logger) {
        _client = client;
        _logger = logger;
    }

    [HttpGet("{text}")]
    public async Task<ActionResult<List<MessageResponse>>> Get(string text, CancellationToken cancellationToken)
    {
        var message = new Message() {  Text = text };
        var response = await _client.SendMessageAsync(message, cancellationToken: cancellationToken);
        _logger.LogInformation("got {response} to {request}", response, message);
        return Ok(response);
    }


    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<MessageResponse>> Post([FromBody] ClientMessage clientMessage, CancellationToken cancellationToken)
    {
        var response = await _client.SendMessageAsync(clientMessage.ToGrpcMessage(), cancellationToken: cancellationToken);
        _logger.LogInformation("got {response} to {request}", response, clientMessage);
        return Ok(response);
    }
}
