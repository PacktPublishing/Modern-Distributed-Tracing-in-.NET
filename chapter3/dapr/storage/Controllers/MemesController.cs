using Microsoft.AspNetCore.Mvc;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private readonly IStorageService _storageService;
    
    public MemesController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("{name}")]
    [Produces("image/png")]
    public async Task<ActionResult> Get(string name, CancellationToken cancellationToken)
    {
        var stream = await _storageService.ReadAsync(name, cancellationToken);
        return new FileStreamResult(stream, "image/png");
    }

    [HttpPut("{name}")]
    [Consumes("image/png")]
    public async Task<ActionResult> Put(string name, CancellationToken cancellationToken) {
        var copy = new MemoryStream();
        await Request.Body.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;

        await _storageService.WriteAsync(name, copy, cancellationToken);
        return Ok();
    }
}
