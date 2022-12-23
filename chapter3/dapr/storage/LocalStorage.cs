namespace storage;

public class LocalStorage : IStorageService
{
    private readonly DaprStorageBinding _binding;

    public LocalStorage(DaprStorageBinding binding)
    {
        _binding = binding;
    }


    public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
    {
        var getFileOperation = new
        {
            operation = "get",
            metadata = new { fileName = name }
        };

        return await _binding.ReadAsync(getFileOperation, cancellationToken);
    }

    public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
    {
        var content = new MemoryStream();
        await stream.CopyToAsync(content, cancellationToken);
        content.Position = 0;

        var createFileOperation = new
        {
            operation = "create",
            data = Convert.ToBase64String(content.ToArray()),
            metadata = new { fileName = name }
        };

        await _binding.WriteAsync(createFileOperation, cancellationToken);
    }
}
