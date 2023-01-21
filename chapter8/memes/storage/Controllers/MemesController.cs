using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private readonly MemeDbContext _dbContext;
    private readonly ILogger<MemesController> _logger;

    public MemesController(MemeDbContext dbContext, ILogger<MemesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("{name}")]
    [Produces("image/png")]
    public async Task<ActionResult> Get(string name, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("memeName", name);
        Meme? meme = await _dbContext.Meme.Where(m => m.Name == name).SingleOrDefaultAsync(cancellationToken);
        if (meme == null) {
            _logger.LogWarning("meme not found");
            return new NotFoundResult();
        }
        
        return new FileStreamResult(new MemoryStream(meme.Data), "image/png");
    }

    [HttpPut("{name}")]
    [Consumes("image/png")]
    public async Task<ActionResult> Put(string name, CancellationToken cancellationToken) {
        using var scope = _logger.BeginScope("memeName", name);
        using var stream = new MemoryStream();
        await Request.Body.CopyToAsync(stream, cancellationToken);

        try
        {
            _dbContext.Meme.Add(new Meme(name, stream.ToArray()));
            await _dbContext.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException e)
        {
            _logger.LogError(e, "failed to upload meme");
            return Conflict();
        }

        return Ok();
    }
}
