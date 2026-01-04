using System;
using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface IRateLimitingService
    {
        Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window);
        Task IncrementAsync(string key, TimeSpan window);
        Task ResetAsync(string key);
        Task<int> GetAttemptsAsync(string key);
    }
}

