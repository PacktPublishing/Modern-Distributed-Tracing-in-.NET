using Memes.OpenTelemetry.Common;
using System.Net.Http.Headers;

namespace frontend;
public class StorageService
{
    private readonly HttpClient _backend;
    private readonly EventService _events;
    public StorageService(IHttpClientFactory httpClientFactory, EventService events)
    {
        _events = events;
        _backend = httpClientFactory.CreateClient("storage");
    }

    public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        var response = await _backend.GetAsync("/memes/" + name, cancellationToken);
        _events.DownloadMemeEvent(name, MemeContentType.PNG, response.Content.Headers.ContentLength);

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task WriteAsync(string name, Stream fileStream, CancellationToken cancellationToken)
    {
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        await _backend.PutAsync("/memes/" + name, content, cancellationToken);
        _events.UploadMemeEvent(name, MemeContentType.PNG, fileStream.Length);
    }
}