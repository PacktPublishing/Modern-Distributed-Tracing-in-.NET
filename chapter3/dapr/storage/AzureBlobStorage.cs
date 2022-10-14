namespace storage
{
    public class AzureBlobStorage : IStorageService
    {
        private readonly DaprStorageBinding _binding;

        public AzureBlobStorage(DaprStorageBinding binding)
        {
            _binding = binding;
        }

        public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
        {
            var getBlobOperation = new
            {
                operation = "get",
                metadata = new { blobName = name }
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
                metadata = new { blobName = name }
            };

            await _binding.WriteAsync(createBlobOperation, cancellationToken);
        }
    }
}
