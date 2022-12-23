using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace storage;

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
        // memes are small, so we can dowanload them to memory
        var blobResult = await _containerClient
            .GetBlobClient(name)
            .DownloadContentAsync(cancellationToken);

        var memeSize = blobResult.Value.Details.ContentLength;
        if (memeSize > 1024 * 1024)
        {
            throw new ArgumentException($"Meme size is too big - {memeSize}, images up to 1MB are supported");
        }

        return blobResult.Value.Content.ToStream();
    }

    public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
    {
        await _containerClient
            .GetBlobClient(name)
            .UploadAsync(stream, cancellationToken);
    }
}
