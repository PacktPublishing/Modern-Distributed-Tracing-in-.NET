using app;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace testing.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
    public static bool configured = false;
    private static readonly ActivitySource Source = new(nameof(DocumentController));
    private readonly IMemoryCache _memoryCache;
    private readonly IStorageService _remoteStorage;
    public DocumentController(IMemoryCache cache, IStorageService remoteStorage)
    {
        _memoryCache = cache;
        _remoteStorage = remoteStorage;
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<string>> Get(string name)
    {
        string? content;
        using (var documentActivity = Source.StartActivity("GetDocument"))
        {
            documentActivity?.AddTag("document.name", name);

            try
            {
                if (_memoryCache.TryGetValue(name, out content))
                {
                    documentActivity?.AddEvent(new ActivityEvent("cache_hit"));
                }
                else
                {
                    documentActivity?.AddEvent(new ActivityEvent("cache_miss"));
                    content = await _remoteStorage.ReadAsync(name);
                }

                if (content != null) documentActivity?.AddTag("document.size", content.Length);
            }
            catch (Exception ex)
            {
                documentActivity?.RecordException(ex);
                documentActivity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }

            // let's end the documentActivity here, so it does not include
            // the rest of request processing
        }

        if (content == null)
        {
            return NotFound();
        }

        return Ok(content);
    }
}
