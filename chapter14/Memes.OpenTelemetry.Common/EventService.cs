using Microsoft.Extensions.Logging;

namespace Memes.OpenTelemetry.Common;

public class EventService
{
    private readonly ILogger<EventService> _logger;
    private static readonly Action<ILogger, string, string?, long?, string, string, Exception> LogDownload = LoggerMessage.Define<string, string?, long?, string, string>(
        LogLevel.Information, new EventId(1), $"download {{{SemanticConventions.MemeNameKey}}} {{{SemanticConventions.MemeTypeKey}}} {{{SemanticConventions.MemeSizeKey}}} {{{SemanticConventions.EventNameKey}}} {{{SemanticConventions.EventDomainKey}}}");

    private static readonly Action<ILogger, string, string?, long?, string, string, Exception> LogUpload = LoggerMessage.Define<string, string?, long?, string, string>(
        LogLevel.Information, new EventId(2), $"upload {{{SemanticConventions.MemeNameKey}}} {{{SemanticConventions.MemeTypeKey}}} {{{SemanticConventions.MemeSizeKey}}} {{{SemanticConventions.EventNameKey}}} {{{SemanticConventions.EventDomainKey}}}");

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public void DownloadMemeEvent(string memeName, 
        MemeContentType type,
        long? memeSize) => 
        LogDownload(_logger, memeName, ContentTypeToString(type), memeSize, SemanticConventions.DownloadMemeEventName, SemanticConventions.MemesEventDomain, default!);

    public void UploadMemeEvent(string memeName, MemeContentType type, long? memeSize) => 
        LogUpload(_logger, memeName, ContentTypeToString(type), memeSize, SemanticConventions.UploadMemeEventName, SemanticConventions.MemesEventDomain, default!);

    private static string? ContentTypeToString(MemeContentType type)
    {
        return type switch
        {
            MemeContentType.PNG => "png",
            MemeContentType.JPG => "jpg",
            _ => "unknown",
        };
    }
}
