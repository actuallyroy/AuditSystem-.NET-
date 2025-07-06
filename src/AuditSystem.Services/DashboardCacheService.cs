using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class DashboardCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<DashboardCacheService> _logger;

        public DashboardCacheService(
            ICacheService cacheService,
            ILogger<DashboardCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        // Dashboard metrics caching
        public async Task<T?> GetDashboardMetricsAsync<T>(Guid? organizationId = null) where T : class
        {
            var cacheKey = CacheKeys.DashboardMetrics(organizationId);
            
            var cachedMetrics = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedMetrics != null)
            {
                _logger.LogDebug("Dashboard metrics found in cache for organization {OrganizationId}", organizationId);
                return cachedMetrics;
            }

            return null;
        }

        public async Task SetDashboardMetricsAsync<T>(T metrics, Guid? organizationId = null) where T : class
        {
            var cacheKey = CacheKeys.DashboardMetrics(organizationId);
            await _cacheService.SetAsync(cacheKey, metrics, CacheKeys.DashboardCacheExpiration);
            
            _logger.LogDebug("Dashboard metrics cached for organization {OrganizationId} for {Expiration} minutes", 
                organizationId, CacheKeys.DashboardCacheExpiration.TotalMinutes);
        }

        // User performance caching
        public async Task<T?> GetUserPerformanceAsync<T>(Guid userId) where T : class
        {
            var cacheKey = CacheKeys.DashboardUserPerformance(userId);
            
            var cachedPerformance = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedPerformance != null)
            {
                _logger.LogDebug("User performance found in cache for user {UserId}", userId);
                return cachedPerformance;
            }

            return null;
        }

        public async Task SetUserPerformanceAsync<T>(Guid userId, T performance) where T : class
        {
            var cacheKey = CacheKeys.DashboardUserPerformance(userId);
            await _cacheService.SetAsync(cacheKey, performance, CacheKeys.DashboardCacheExpiration);
            
            _logger.LogDebug("User performance cached for user {UserId} for {Expiration} minutes", 
                userId, CacheKeys.DashboardCacheExpiration.TotalMinutes);
        }

        // Template statistics caching
        public async Task<T?> GetTemplateStatsAsync<T>(Guid templateId) where T : class
        {
            var cacheKey = CacheKeys.DashboardTemplateStats(templateId);
            
            var cachedStats = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedStats != null)
            {
                _logger.LogDebug("Template statistics found in cache for template {TemplateId}", templateId);
                return cachedStats;
            }

            return null;
        }

        public async Task SetTemplateStatsAsync<T>(Guid templateId, T stats) where T : class
        {
            var cacheKey = CacheKeys.DashboardTemplateStats(templateId);
            await _cacheService.SetAsync(cacheKey, stats, CacheKeys.DashboardCacheExpiration);
            
            _logger.LogDebug("Template statistics cached for template {TemplateId} for {Expiration} minutes", 
                templateId, CacheKeys.DashboardCacheExpiration.TotalMinutes);
        }

        // Audit trends caching
        public async Task<T?> GetAuditTrendsAsync<T>(Guid? organizationId = null) where T : class
        {
            var cacheKey = CacheKeys.DashboardAuditTrends(organizationId);
            
            var cachedTrends = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedTrends != null)
            {
                _logger.LogDebug("Audit trends found in cache for organization {OrganizationId}", organizationId);
                return cachedTrends;
            }

            return null;
        }

        public async Task SetAuditTrendsAsync<T>(T trends, Guid? organizationId = null) where T : class
        {
            var cacheKey = CacheKeys.DashboardAuditTrends(organizationId);
            await _cacheService.SetAsync(cacheKey, trends, CacheKeys.DashboardCacheExpiration);
            
            _logger.LogDebug("Audit trends cached for organization {OrganizationId} for {Expiration} minutes", 
                organizationId, CacheKeys.DashboardCacheExpiration.TotalMinutes);
        }

        // Cache invalidation methods
        public async Task InvalidateAllDashboardCacheAsync()
        {
            await _cacheService.RemoveByPatternAsync(CacheKeys.DashboardPattern());
            _logger.LogDebug("All dashboard cache invalidated");
        }

        public async Task InvalidateOrganizationDashboardCacheAsync(Guid organizationId)
        {
            await _cacheService.RemoveAsync(CacheKeys.DashboardMetrics(organizationId));
            await _cacheService.RemoveAsync(CacheKeys.DashboardAuditTrends(organizationId));
            _logger.LogDebug("Dashboard cache invalidated for organization {OrganizationId}", organizationId);
        }

        public async Task InvalidateUserPerformanceCacheAsync(Guid userId)
        {
            await _cacheService.RemoveAsync(CacheKeys.DashboardUserPerformance(userId));
            _logger.LogDebug("User performance cache invalidated for user {UserId}", userId);
        }

        public async Task InvalidateTemplateCacheAsync(Guid templateId)
        {
            await _cacheService.RemoveAsync(CacheKeys.DashboardTemplateStats(templateId));
            _logger.LogDebug("Template statistics cache invalidated for template {TemplateId}", templateId);
        }

        // Warm up cache methods
        public async Task WarmUpDashboardCacheAsync<T>(T metrics, Guid? organizationId = null) where T : class
        {
            await SetDashboardMetricsAsync(metrics, organizationId);
            _logger.LogDebug("Dashboard cache warmed up for organization {OrganizationId}", organizationId);
        }

        // Batch cache operations
        public async Task SetMultipleUserPerformanceAsync<T>(Dictionary<Guid, T> userPerformances) where T : class
        {
            var cacheData = new Dictionary<string, T>();
            foreach (var kvp in userPerformances)
            {
                cacheData[CacheKeys.DashboardUserPerformance(kvp.Key)] = kvp.Value;
            }

            await _cacheService.SetMultipleAsync(cacheData, CacheKeys.DashboardCacheExpiration);
            _logger.LogDebug("Multiple user performances cached for {Count} users", userPerformances.Count);
        }

        public async Task SetMultipleTemplateStatsAsync<T>(Dictionary<Guid, T> templateStats) where T : class
        {
            var cacheData = new Dictionary<string, T>();
            foreach (var kvp in templateStats)
            {
                cacheData[CacheKeys.DashboardTemplateStats(kvp.Key)] = kvp.Value;
            }

            await _cacheService.SetMultipleAsync(cacheData, CacheKeys.DashboardCacheExpiration);
            _logger.LogDebug("Multiple template statistics cached for {Count} templates", templateStats.Count);
        }

        // Health check for dashboard cache
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var healthKey = CacheKeys.HealthCheck();
                var testData = new { Health = "OK", Timestamp = DateTime.UtcNow };
                
                await _cacheService.SetAsync(healthKey, testData, TimeSpan.FromMinutes(1));
                var retrieved = await _cacheService.GetAsync<object>(healthKey);
                
                return retrieved != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard cache health check failed");
                return false;
            }
        }
    }

    // DTOs for dashboard metrics
    public class DashboardMetrics
    {
        public int TotalAudits { get; set; }
        public int CompletedAudits { get; set; }
        public int PendingAudits { get; set; }
        public double AverageScore { get; set; }
        public int CriticalIssues { get; set; }
        public double CompletionRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UserPerformance
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int AuditsCompleted { get; set; }
        public double AverageScore { get; set; }
        public int IssuesFound { get; set; }
        public double PerformanceRating { get; set; }
        public DateTime LastAuditDate { get; set; }
    }

    public class TemplateStats
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TimesUsed { get; set; }
        public double AverageScore { get; set; }
        public int IssuesFound { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class AuditTrends
    {
        public List<TrendData> CompletionTrends { get; set; } = new();
        public List<TrendData> ScoreTrends { get; set; } = new();
        public List<TrendData> IssueTrends { get; set; } = new();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class TrendData
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
} 