using System.Diagnostics;
using System.Net.Http.Headers;

namespace frontend;
public partial class StorageService
{
    private static readonly ActivitySource Source = new ("Storage");
    private readonly HttpClient _backend;
    private readonly ILogger<StorageService> _logger;
    public StorageService(IHttpClientFactory httpClientFactory, ILogger<StorageService> logger)
    {
        _backend = httpClientFactory.CreateClient("storage");
        _logger = logger;
    }

    public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("get meme");
        
        var response = await _backend.GetAsync("/memes/" + name, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to download meme");
        }

        DownloadMemeEvent(response.Content.Headers.ContentLength, name);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task WriteAsync(string name, Stream fileStream, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("put meme");
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var response = await _backend.PutAsync("/memes/" + name, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to upload meme");
        }

        UploadMemeEvent(fileStream.Length, name);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "download {memeSize} {memeName}")]
    private partial void DownloadMemeEvent(long? memeSize, string memeName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "upload {memeSize} {memeName}")]
    private partial void UploadMemeEvent(long memeSize, string memeName);
}