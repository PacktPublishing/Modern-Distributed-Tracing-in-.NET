using Microsoft.Extensions.Options;

namespace storage
{
    public class DaprStorageBinding
    {
        private readonly HttpClient _daprClient;
        private readonly string _bindingName;

        public DaprStorageBinding(IHttpClientFactory httpClientFactory, IOptions<CloudStorageOptions> options)
        {
            _daprClient = httpClientFactory.CreateClient("daprBindings");
            _bindingName = options.Value.Type.ToString().ToLowerInvariant();
        }

        public async Task<Stream> ReadAsync<T>(T getOperation, CancellationToken cancellationToken)
        {
            var getBlobResult = await _daprClient.PostAsJsonAsync(_bindingName, getOperation, cancellationToken);
            getBlobResult.EnsureSuccessStatusCode();

            var content = new MemoryStream();
            await getBlobResult.Content.CopyToAsync(content, cancellationToken);
            content.Position = 0;

            return content;
        }

        public async Task WriteAsync<T>(T createOperation, CancellationToken cancellationToken)
        {
            var createBlobResult = await _daprClient.PostAsJsonAsync(_bindingName, createOperation, cancellationToken);
            createBlobResult.EnsureSuccessStatusCode();
        }
    }
}
