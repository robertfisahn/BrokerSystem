using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BrokerSystem.Api.Common.Caching
{
    public class InMemoryCacheService(IMemoryCache cache) : ICacheService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (cache.TryGetValue(key, out T? result) && result is not null)
            {
                return result;
            }

            var lockObject = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await lockObject.WaitAsync();

            try
            {
                if (cache.TryGetValue(key, out result) && result is not null)
                {
                    return result;
                }

                result = await factory();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
                };

                cache.Set(key, result, cacheOptions);

                return result;
            }
            finally
            {
                lockObject.Release();
            }
        }

        public void Remove(string key)
        {
            cache.Remove(key);
        }
    }
}
