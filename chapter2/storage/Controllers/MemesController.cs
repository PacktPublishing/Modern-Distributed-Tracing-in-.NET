using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, byte[]> _storage = new();
    private readonly ILogger<MemesController> _logger;

    public MemesController(ILogger<MemesController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{name}")]
    [Produces("image/png")]
    public ActionResult Get(string name, CancellationToken cancellationToken)
    {
        if (_storage.TryGetValue(name, out var data)) {
            _logger.LogInformation("Returning '{name}', {size} bytes", name, data.Length);
            return new FileStreamResult(new MemoryStream(data), "image/png");
        }

        _logger.LogWarning("Meme '{name}' not found", name);
        return new NotFoundResult();
    }

    [HttpPut("{name}")]
    [Consumes("image/png")]
    public async Task<ActionResult> Put(string name, CancellationToken cancellationToken) {
        using var stream = new MemoryStream();
        await Request.Body.CopyToAsync(stream);
        if (_storage.TryAdd(name, stream.ToArray())) {
            _logger.LogInformation("Uploading '{name}', {size} bytes", name, stream.Length);
            return Ok();
        } 

        _logger.LogWarning("Meme '{name}' already exists", name);
        return Conflict();
    }
}
