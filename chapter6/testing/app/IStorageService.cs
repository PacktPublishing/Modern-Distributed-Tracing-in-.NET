namespace app;

public interface IStorageService
{
    Task<string?> ReadAsync(string name);
    Task WriteAsync(string name, string value);
}
