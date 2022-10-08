using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace storage.Controllers;

[ApiController]
[Route("[controller]")]
public class MemesController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IDatabase _cache;
    private readonly ILogger<MemesController> _logger;
    private static readonly TimeSpan ExpirationTime = TimeSpan.FromSeconds(10);

    public MemesController(IStorageService storageService, IDatabase cache, ILogger<MemesController> logger)
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

            await _cache.StringSetAsync(name, copy.ToArray(), ExpirationTime);
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
        await _cache.StringSetAsync(name, copy.ToArray(), ExpirationTime);

        _logger.LogInformation("Uploaded '{name}', {size} bytes", name, copy.Length);

        return Ok();
    }

    private async Task<Stream?> GetFromRedisAsync(string name)
    {
        try
        {
            var redisData = await _cache.StringGetAsync(name);
            if (redisData.HasValue)
            {
                return new MemoryStream((byte[])redisData);
            }
        }
        catch (Exception ex) when (ex is RedisException || ex is RedisTimeoutException)
        {
            _logger.LogError(ex, "Failed to get data from redis");
        }

        return null;
    }
}
