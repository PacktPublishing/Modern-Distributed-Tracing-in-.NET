using Memes.OpenTelemetry.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private readonly MemeDbContext _dbContext;
    private readonly ILogger<MemesController> _logger;
    private readonly EventService _events;

    public MemesController(MemeDbContext dbContext, EventService events,  ILogger<MemesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _events = events;
    }

    [HttpGet("{name}")]
    [Produces("image/png")]
    public async Task<ActionResult> Get(string name, CancellationToken cancellationToken)
    {
        Meme? meme = await _dbContext.Meme.Where(m => m.Name == name).SingleOrDefaultAsync(cancellationToken);
        if (meme == null) {
            _logger.LogWarning("Meme '{meme_name}' not found", name);
            return new NotFoundResult();
        }

        _events.DownloadMemeEvent(name, MemeContentType.PNG, meme.Data.Length);
        return new FileStreamResult(new MemoryStream(meme.Data), "image/png");
    }

    [HttpPut("{name}")]
    [Consumes("image/png")]
    public async Task<ActionResult> Put(string name, CancellationToken cancellationToken) {
        using var stream = new MemoryStream();
        await Request.Body.CopyToAsync(stream, cancellationToken);

        try
        {
            _dbContext.Meme.Add(new Meme(name, stream.ToArray()));
            await _dbContext.SaveChangesAsync(cancellationToken);

            _events.UploadMemeEvent(name, MemeContentType.PNG, stream.Length);
        } catch (DbUpdateException)
        {
            return Conflict();
        }

        return Ok();
    }
}
