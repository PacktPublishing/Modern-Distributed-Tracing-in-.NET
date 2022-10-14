namespace storage
{
    public interface IStorageService
    {
        Task<Stream> ReadAsync(string id, CancellationToken cancellationToken);

        Task WriteAsync(string id, Stream data, CancellationToken cancellationToken);
    }
}
