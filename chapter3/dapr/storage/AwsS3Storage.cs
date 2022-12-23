namespace storage;

public class AwsS3Storage : IStorageService
{
    private readonly DaprStorageBinding _binding;

    public AwsS3Storage(DaprStorageBinding binding)
    {
        _binding = binding;
    }

    public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        var getBlobOperation = new
        {
            operation = "get",
            metadata = new { key = name }
        };

        return await _binding.ReadAsync(getBlobOperation, cancellationToken);
    }

    public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
    {
        var content = new MemoryStream();
        await stream.CopyToAsync(content, cancellationToken);
        content.Position = 0;

        var createBlobOperation = new
        {
            operation = "create",
            data = Convert.ToBase64String(content.ToArray()),
            metadata = new { key = name }
        };

        await _binding.WriteAsync(createBlobOperation, cancellationToken);
    }
}
