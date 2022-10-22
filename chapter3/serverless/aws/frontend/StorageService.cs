using System.Net.Http.Headers;

namespace frontend;
public class StorageService
{
    private readonly HttpClient _backend;
    public StorageService(IHttpClientFactory httpClientFactory)
    {
        _backend = httpClientFactory.CreateClient("storage");
    }

    public Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        return _backend.GetStreamAsync("/?name=" + name, cancellationToken);
    }

    public async Task WriteAsync(string name, Stream fileStream, CancellationToken cancellationToken)
    {
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        await _backend.PutAsync("/?name=" + name, content, cancellationToken);
    }
}