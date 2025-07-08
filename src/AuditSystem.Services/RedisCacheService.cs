using AuditSystem.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _redis = redis;
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedValue))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache value for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
                
                var options = new DistributedCacheEntryOptions();
                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Set default expiration of 1 hour if not specified
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                // Add the AuditSystem prefix to the pattern since IDistributedCache adds it automatically
                var prefixedPattern = $"AuditSystem:{pattern}";
                
                _logger.LogInformation("Attempting to remove cache keys with pattern: {Pattern} (prefixed: {PrefixedPattern})", pattern, prefixedPattern);
                
                var keys = server.Keys(pattern: prefixedPattern);
                
                var keyArray = keys.ToArray();
                _logger.LogInformation("Found {Count} keys matching pattern: {PrefixedPattern}", keyArray.Length, prefixedPattern);
                
                if (keyArray.Any())
                {
                    foreach (var key in keyArray)
                    {
                        _logger.LogInformation("Found key to remove: {Key}", key);
                    }
                    
                    await _database.KeyDeleteAsync(keyArray);
                    _logger.LogInformation("Successfully removed {Count} cache keys matching pattern: {Pattern} (prefixed: {PrefixedPattern})", keyArray.Length, pattern, prefixedPattern);
                }
                else
                {
                    _logger.LogWarning("No cache keys found matching pattern: {Pattern} (prefixed: {PrefixedPattern})", pattern, prefixedPattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache values by pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
                return false;
            }
        }

        public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class
        {
            var result = new Dictionary<string, T?>();
            
            try
            {
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var values = await _database.StringGetAsync(redisKeys);
                
                for (int i = 0; i < redisKeys.Length; i++)
                {
                    var key = redisKeys[i];
                    var value = values[i];
                    
                    if (value.HasValue)
                    {
                        try
                        {
                            result[key] = JsonSerializer.Deserialize<T>(value, JsonOptions);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Error deserializing cache value for key: {Key}", key);
                            result[key] = null;
                        }
                    }
                    else
                    {
                        result[key] = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving multiple cache values");
                // Return empty dictionary on error
                foreach (var key in keys)
                {
                    result[key] = null;
                }
            }
            
            return result;
        }

        public async Task SetMultipleAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var tasks = keyValues.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple cache values");
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null)
        {
            try
            {
                var result = await _database.StringIncrementAsync(key, value);
                
                if (expiration.HasValue)
                {
                    await _database.KeyExpireAsync(key, expiration.Value);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing cache value for key: {Key}", key);
                return 0;
            }
        }

        public async Task<bool> ExpireAsync(string key, TimeSpan expiration)
        {
            try
            {
                return await _database.KeyExpireAsync(key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting expiration for cache key: {Key}", key);
                return false;
            }
        }

        // Additional helper methods for advanced Redis operations
        public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
                var result = await _database.StringSetAsync(key, serializedValue, expiration, When.NotExists);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value if not exists for key: {Key}", key);
                return false;
            }
        }

        public async Task<T?> GetAndRefreshAsync<T>(string key, TimeSpan expiration) where T : class
        {
            try
            {
                var value = await GetAsync<T>(key);
                if (value != null)
                {
                    await ExpireAsync(key, expiration);
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting and refreshing cache value for key: {Key}", key);
                return null;
            }
        }

        public async Task<List<string>> GetKeysByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);
                return keys.Select(k => k.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
                return new List<string>();
            }
        }
    }
} 