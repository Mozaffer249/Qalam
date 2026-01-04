using Microsoft.Extensions.Caching.Memory;
using Qalam.Service.Abstracts;
using System;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class RateLimitingService : IRateLimitingService
    {
        private readonly IMemoryCache _cache;

        public RateLimitingService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window)
        {
            var attempts = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return 0;
            });

            return Task.FromResult(attempts < maxAttempts);
        }

        public Task IncrementAsync(string key, TimeSpan window)
        {
            var attempts = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return 0;
            });

            _cache.Set(key, attempts + 1, window);
            return Task.CompletedTask;
        }

        public Task ResetAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<int> GetAttemptsAsync(string key)
        {
            var attempts = _cache.Get<int?>(key) ?? 0;
            return Task.FromResult(attempts);
        }
    }
}
