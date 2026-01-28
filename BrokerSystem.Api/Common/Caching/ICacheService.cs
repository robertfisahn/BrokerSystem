using System;
using System.Threading.Tasks;

namespace BrokerSystem.Api.Common.Caching
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Remove(string key);
    }
}
