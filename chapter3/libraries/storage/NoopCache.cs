using Microsoft.Extensions.Caching.Distributed;

namespace storage
{
    public class NoopCache : IDistributedCache
    {
        private readonly Task<byte[]?> NullResult = Task.FromResult((byte[]?)null);
        public byte[]? Get(string key)
        {
            return null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return NullResult;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return NullResult;
        }

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return NullResult;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            return NullResult;
        }
    }
}
