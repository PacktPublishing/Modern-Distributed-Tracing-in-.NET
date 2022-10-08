using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace storage
{
    public class AzureBlobStorage : IStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly AzureBlobStorageOptions _options;
        public AzureBlobStorage(IOptions<AzureBlobStorageOptions> options)
        {
            _options = options.Value;
            var storageClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = storageClient.GetBlobContainerClient(_options.Container);
            _containerClient.CreateIfNotExists();
        }


        public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(name);
            var blobResult = await blobClient.DownloadContentAsync(cancellationToken);
            return blobResult.Value.Content.ToStream();
        }

        public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(name);
            await blobClient.UploadAsync(stream, cancellationToken);
        }
    }
}
