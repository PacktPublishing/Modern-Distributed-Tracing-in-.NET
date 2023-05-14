namespace Memes.OpenTelemetry.Common;

public class SemanticConventions
{
    // custom attributes:
    public const string MemeNameKey = "memes.meme.name";
    public const string MemeSizeKey = "memes.meme.size";
    public const string MemeTypeKey = "memes.meme.type";

    // custom events:
    public const string UploadMemeEventName = "upload_meme";
    public const string DownloadMemeEventName = "download_meme";
    public const string MemesEventDomain = "memes";

    // Standard OpenTelemetry Semantic attributes (as of version 1.20.0):
    public const string EnvironmentKey = "environment";

    public const string HttpResendCountKey = "http.resend_count";
    public const string HttpRequestLengthKey = "http.request_content_length";
    public const string HttpResponseLengthKey = "http.response_content_length";

    public const string EventNameKey = "event.name";
    public const string EventDomainKey = "event.domain";
}
