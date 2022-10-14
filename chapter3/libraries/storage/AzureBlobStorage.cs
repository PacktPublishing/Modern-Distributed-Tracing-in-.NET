using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace storage
{
    public class AzureBlobStorage : IStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly AzureBlobOptions _options;
        public AzureBlobStorage(IOptions<CloudStorageOptions> options)
        {
            _options = options.Value.AzureBlob ?? throw new ArgumentNullException(nameof(options.Value.AzureBlob));
            var storageClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = storageClient.GetBlobContainerClient(_options.Container);
            _containerClient.CreateIfNotExists();
        }


        public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
        {
            var blobResult = await _containerClient
                .GetBlobClient(name)
                .DownloadContentAsync(cancellationToken);
            
            return blobResult.Value.Content.ToStream();
        }

        public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            await _containerClient
                .GetBlobClient(name)
                .UploadAsync(stream, cancellationToken);
        }
    }
}
