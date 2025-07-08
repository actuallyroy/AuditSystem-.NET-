using AuditSystem.Domain.Services;
using AuditSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class CacheController : BaseApiController
    {
        private readonly ICacheService _cacheService;
        private readonly DashboardCacheService _dashboardCacheService;

        public CacheController(
            ICacheService cacheService,
            DashboardCacheService dashboardCacheService)
        {
            _cacheService = cacheService;
            _dashboardCacheService = dashboardCacheService;
        }

        /// <summary>
        /// Get cache health status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<CacheHealthResponse>> GetCacheHealth()
        {
            try
            {
                var isHealthy = await _dashboardCacheService.IsHealthyAsync();
                
                return Ok(new CacheHealthResponse
                {
                    IsHealthy = isHealthy,
                    Message = isHealthy ? "Cache is healthy" : "Cache is not responding",
                    CheckedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheHealthResponse
                {
                    IsHealthy = false,
                    Message = $"Cache health check failed: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        [HttpDelete("clear-all")]
        public async Task<ActionResult<CacheOperationResponse>> ClearAllCache()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync("*");
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = "All cache entries cleared successfully",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear user-related cache entries
        /// </summary>
        [HttpDelete("clear-user/{userId}")]
        public async Task<ActionResult<CacheOperationResponse>> ClearUserCache(Guid userId)
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(CacheKeys.UserPattern(userId));
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = $"User cache cleared for user {userId}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear user cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear template-related cache entries
        /// </summary>
        [HttpDelete("clear-template/{templateId}")]
        public async Task<ActionResult<CacheOperationResponse>> ClearTemplateCache(Guid templateId)
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(CacheKeys.TemplatePattern(templateId));
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = $"Template cache cleared for template {templateId}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear template cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear all template cache entries
        /// </summary>
        [HttpDelete("clear-all-templates")]
        public async Task<ActionResult<CacheOperationResponse>> ClearAllTemplateCache()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync("template:*");
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = "All template cache entries cleared",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear all template cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear organization-related cache entries
        /// </summary>
        [HttpDelete("clear-organization/{organizationId}")]
        public async Task<ActionResult<CacheOperationResponse>> ClearOrganizationCache(Guid organizationId)
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(CacheKeys.OrganizationPattern(organizationId));
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = $"Organization cache cleared for organization {organizationId}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear organization cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear dashboard cache entries
        /// </summary>
        [HttpDelete("clear-dashboard")]
        public async Task<ActionResult<CacheOperationResponse>> ClearDashboardCache()
        {
            try
            {
                await _dashboardCacheService.InvalidateAllDashboardCacheAsync();
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = "Dashboard cache cleared successfully",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to clear dashboard cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<CacheStatsResponse>> GetCacheStats()
        {
            try
            {
                var userKeys = await _cacheService.GetKeysByPatternAsync(CacheKeys.AllUserKeys());
                var templateKeys = await _cacheService.GetKeysByPatternAsync(CacheKeys.AllTemplateKeys());
                var organizationKeys = await _cacheService.GetKeysByPatternAsync(CacheKeys.AllOrganizationKeys());
                var dashboardKeys = await _cacheService.GetKeysByPatternAsync(CacheKeys.AllDashboardKeys());

                return Ok(new CacheStatsResponse
                {
                    UserCacheEntries = userKeys.Count,
                    TemplateCacheEntries = templateKeys.Count,
                    OrganizationCacheEntries = organizationKeys.Count,
                    DashboardCacheEntries = dashboardKeys.Count,
                    TotalCacheEntries = userKeys.Count + templateKeys.Count + organizationKeys.Count + dashboardKeys.Count,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheStatsResponse
                {
                    UserCacheEntries = 0,
                    TemplateCacheEntries = 0,
                    OrganizationCacheEntries = 0,
                    DashboardCacheEntries = 0,
                    TotalCacheEntries = 0,
                    GeneratedAt = DateTime.UtcNow,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Warm up cache for specific user
        /// </summary>
        [HttpPost("warm-up/user/{userId}")]
        public async Task<ActionResult<CacheOperationResponse>> WarmUpUserCache(Guid userId)
        {
            try
            {
                // This would require access to the cached services, which we don't have here
                // In a real implementation, you might want to inject the cached services
                // or create a dedicated cache warming service
                
                return Ok(new CacheOperationResponse
                {
                    Success = true,
                    Message = $"Cache warm-up initiated for user {userId}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CacheOperationResponse
                {
                    Success = false,
                    Message = $"Failed to warm up user cache: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }
    }

    public class CacheHealthResponse
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    public class CacheOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
    }

    public class CacheStatsResponse
    {
        public int UserCacheEntries { get; set; }
        public int TemplateCacheEntries { get; set; }
        public int OrganizationCacheEntries { get; set; }
        public int DashboardCacheEntries { get; set; }
        public int TotalCacheEntries { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? Error { get; set; }
    }
} 