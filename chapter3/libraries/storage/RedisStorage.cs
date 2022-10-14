using StackExchange.Redis;

namespace storage
{
    public class RedisStorage : IStorageService
    {
        private readonly IDatabase _cache;
        private static readonly TimeSpan ExpirationTime = TimeSpan.FromSeconds(60);

        public RedisStorage(IDatabase cache)
        {
            _cache = cache;
        }

        public async Task<Stream> ReadAsync(string name, CancellationToken cancellationToken)
        {
            var redisData = await _cache.StringGetAsync(name);
            if (redisData.HasValue)
            {
                return new MemoryStream((byte[])redisData);
            }
            else
            {
                throw new Exception("Can't find meme " + name);
            }
        }

        public async Task WriteAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            await _cache.StringSetAsync(name, copy.ToArray(), ExpirationTime);

        }
    }
}
