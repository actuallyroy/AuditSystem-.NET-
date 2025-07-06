using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class NullCacheService : ICacheService
    {
        private readonly ILogger<NullCacheService> _logger;

        public NullCacheService(ILogger<NullCacheService> logger)
        {
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            _logger.LogDebug("Cache miss (null cache): {Key}", key);
            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            _logger.LogDebug("Cache set ignored (null cache): {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _logger.LogDebug("Cache remove ignored (null cache): {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            _logger.LogDebug("Cache pattern remove ignored (null cache): {Pattern}", pattern);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(false);
        }

        public Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class
        {
            var result = new Dictionary<string, T?>();
            foreach (var key in keys)
            {
                result[key] = null;
            }
            return Task.FromResult(result);
        }

        public Task SetMultipleAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiration = null) where T : class
        {
            _logger.LogDebug("Cache multiple set ignored (null cache): {Count} keys", keyValues.Count);
            return Task.CompletedTask;
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null)
        {
            return Task.FromResult(0L);
        }

        public Task<bool> ExpireAsync(string key, TimeSpan expiration)
        {
            return Task.FromResult(false);
        }

        public Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            return Task.FromResult(false);
        }

        public Task<T?> GetAndRefreshAsync<T>(string key, TimeSpan expiration) where T : class
        {
            return Task.FromResult<T?>(null);
        }

        public Task<List<string>> GetKeysByPatternAsync(string pattern)
        {
            return Task.FromResult(new List<string>());
        }
    }
} 