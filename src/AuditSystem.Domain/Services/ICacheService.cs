using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface ICacheService
    {
        /// <summary>
        /// Get a value from cache
        /// </summary>
        Task<T?> GetAsync<T>(string key) where T : class;
        
        /// <summary>
        /// Set a value in cache with expiration
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        
        /// <summary>
        /// Remove a value from cache
        /// </summary>
        Task RemoveAsync(string key);
        
        /// <summary>
        /// Remove multiple values from cache by pattern
        /// </summary>
        Task RemoveByPatternAsync(string pattern);
        
        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key);
        
        /// <summary>
        /// Get multiple values from cache
        /// </summary>
        Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class;
        
        /// <summary>
        /// Set multiple values in cache
        /// </summary>
        Task SetMultipleAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiration = null) where T : class;
        
        /// <summary>
        /// Increment a numeric value in cache
        /// </summary>
        Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null);
        
        /// <summary>
        /// Set cache expiration for a key
        /// </summary>
        Task<bool> ExpireAsync(string key, TimeSpan expiration);
        
        /// <summary>
        /// Get all cache keys matching a pattern
        /// </summary>
        Task<List<string>> GetKeysByPatternAsync(string pattern);
    }
} 