using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MemesController> _logger;
    
    public MemesController(IStorageService storageService, IDistributedCache cache, ILogger<MemesController> logger)
    {
        _storageService = storageService;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("{name}")]
    [Produces("image/png")]
    public async Task<ActionResult> Get(string name, CancellationToken cancellationToken)
    {
        var cachedStream = await GetFromRedisAsync(name);
        if (cachedStream == null)
        {
            using var stream = await _storageService.ReadAsync(name, cancellationToken);
            var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;

            await _cache.SetAsync(name, copy.ToArray());
            copy.Position = 0;
            cachedStream = copy;
        }
        _logger.LogInformation("Returning '{meme}', {size} bytes", name, cachedStream.Length);
        return new FileStreamResult(cachedStream, "image/png");
    }

    [HttpPut("{name}")]
    [Consumes("image/png")]
    public async Task<ActionResult> Put(string name, CancellationToken cancellationToken) {
        var copy = new MemoryStream();
        await Request.Body.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;

        await _storageService.WriteAsync(name, copy, cancellationToken);
        
        copy.Position = 0;
        await _cache.SetAsync(name, copy.ToArray());

        _logger.LogInformation("Uploaded '{name}', {size} bytes", name, copy.Length);

        return Ok();
    }

    private async Task<Stream?> GetFromRedisAsync(string name)
    {
        try
        {
            var redisData = await _cache.GetAsync(name);
            if (redisData != null)
            {
                return new MemoryStream(redisData);
            }
        }
        catch (Exception ex) when (ex is RedisException || ex is RedisTimeoutException)
        {
            _logger.LogError(ex, "Failed to get data from redis");
        }

        return null;
    }
}
