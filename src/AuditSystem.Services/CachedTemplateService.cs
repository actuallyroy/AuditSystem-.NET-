using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class CachedTemplateService : ITemplateService
    {
        private readonly ITemplateService _templateService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedTemplateService> _logger;

        public CachedTemplateService(
            ITemplateService templateService,
            ICacheService cacheService,
            ILogger<CachedTemplateService> logger)
        {
            _templateService = templateService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Template> GetTemplateByIdAsync(Guid templateId)
        {
            var cacheKey = CacheKeys.TemplateById(templateId);
            
            var cachedTemplate = await _cacheService.GetAsync<Template>(cacheKey);
            if (cachedTemplate != null)
            {
                _logger.LogDebug("Template {TemplateId} found in cache", templateId);
                return cachedTemplate;
            }

            var template = await _templateService.GetTemplateByIdAsync(templateId);
            if (template != null)
            {
                await _cacheService.SetAsync(cacheKey, template, CacheKeys.TemplateCacheExpiration);
                _logger.LogDebug("Template {TemplateId} cached for {Expiration} minutes", 
                    templateId, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return template;
        }

        public async Task<IEnumerable<Template>> GetAllTemplatesAsync()
        {
            // For admin operations, don't cache all templates as this can be a large dataset
            return await _templateService.GetAllTemplatesAsync();
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync()
        {
            var cacheKey = CacheKeys.PublishedTemplates();
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogDebug("Published templates found in cache");
                return cachedTemplates;
            }

            var templates = await _templateService.GetPublishedTemplatesAsync();
            var templatesList = templates.ToList();

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                
                // Also cache individual templates
                foreach (var template in templatesList)
                {
                    await _cacheService.SetAsync(CacheKeys.TemplateById(template.TemplateId), template, CacheKeys.TemplateCacheExpiration);
                }
                
                _logger.LogDebug("Published templates cached for {Expiration} minutes", 
                    CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync(Guid userId)
        {
            var cacheKey = CacheKeys.PublishedTemplatesByUser(userId);
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogDebug("Published templates for user {UserId} found in cache", userId);
                return cachedTemplates;
            }

            var templates = await _templateService.GetPublishedTemplatesAsync(userId);
            var templatesList = templates.ToList();

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                
                // Also cache individual templates
                foreach (var template in templatesList)
                {
                    await _cacheService.SetAsync(CacheKeys.TemplateById(template.TemplateId), template, CacheKeys.TemplateCacheExpiration);
                }
                
                _logger.LogDebug("Published templates for user {UserId} cached for {Expiration} minutes", 
                    userId, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<IEnumerable<Template>> GetTemplatesByUserAsync(Guid userId)
        {
            var cacheKey = CacheKeys.TemplatesByUser(userId);
            _logger.LogInformation("Getting templates for user {UserId} with cache key: {CacheKey}", userId, cacheKey);
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogInformation("Templates for user {UserId} found in cache, returning {Count} templates", userId, cachedTemplates.Count);
                return cachedTemplates;
            }

            _logger.LogInformation("Templates for user {UserId} not found in cache, fetching from database", userId);
            var templates = await _templateService.GetTemplatesByUserAsync(userId);
            var templatesList = templates.ToList();

            _logger.LogInformation("Fetched {Count} templates from database for user {UserId}", templatesList.Count, userId);

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                
                // Also cache individual templates
                foreach (var template in templatesList)
                {
                    await _cacheService.SetAsync(CacheKeys.TemplateById(template.TemplateId), template, CacheKeys.TemplateCacheExpiration);
                }
                
                _logger.LogInformation("Templates for user {UserId} cached for {Expiration} minutes", 
                    userId, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category)
        {
            var cacheKey = CacheKeys.TemplatesByCategory(category);
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogDebug("Templates for category {Category} found in cache", category);
                return cachedTemplates;
            }

            var templates = await _templateService.GetTemplatesByCategoryAsync(category);
            var templatesList = templates.ToList();

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                _logger.LogDebug("Templates for category {Category} cached for {Expiration} minutes", 
                    category, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category, Guid userId)
        {
            var cacheKey = CacheKeys.TemplatesByCategoryAndUser(category, userId);
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogDebug("Templates for category {Category} and user {UserId} found in cache", category, userId);
                return cachedTemplates;
            }

            var templates = await _templateService.GetTemplatesByCategoryAsync(category, userId);
            var templatesList = templates.ToList();

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                _logger.LogDebug("Templates for category {Category} and user {UserId} cached for {Expiration} minutes", 
                    category, userId, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId)
        {
            var cacheKey = CacheKeys.AssignedTemplates(auditorId);
            
            var cachedTemplates = await _cacheService.GetAsync<List<Template>>(cacheKey);
            if (cachedTemplates != null)
            {
                _logger.LogDebug("Assigned templates for auditor {AuditorId} found in cache", auditorId);
                return cachedTemplates;
            }

            var templates = await _templateService.GetAssignedTemplatesAsync(auditorId);
            var templatesList = templates.ToList();

            if (templatesList.Any())
            {
                await _cacheService.SetAsync(cacheKey, templatesList, CacheKeys.TemplateCacheExpiration);
                _logger.LogDebug("Assigned templates for auditor {AuditorId} cached for {Expiration} minutes", 
                    auditorId, CacheKeys.TemplateCacheExpiration.TotalMinutes);
            }

            return templatesList;
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            _logger.LogInformation("Creating template: {TemplateName} for user {UserId}", template.Name, template.CreatedById);
            
            var createdTemplate = await _templateService.CreateTemplateAsync(template);
            
            _logger.LogInformation("Template {TemplateId} created in database, now updating cache", createdTemplate.TemplateId);
            
            // Cache the new template
            await _cacheService.SetAsync(CacheKeys.TemplateById(createdTemplate.TemplateId), createdTemplate, CacheKeys.TemplateCacheExpiration);
            _logger.LogInformation("Template {TemplateId} cached individually", createdTemplate.TemplateId);
            
            // Invalidate related caches
            _logger.LogInformation("Invalidating user templates cache for user {UserId}", createdTemplate.CreatedById);
            await InvalidateUserTemplatesCacheAsync(createdTemplate.CreatedById);
            
            if (!string.IsNullOrEmpty(createdTemplate.Category))
            {
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategory(createdTemplate.Category));
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategoryAndUser(createdTemplate.Category, createdTemplate.CreatedById));
                _logger.LogInformation("Category cache invalidated for category {Category}", createdTemplate.Category);
            }
            
            _logger.LogInformation("Template {TemplateId} created and cache updated successfully", createdTemplate.TemplateId);
            return createdTemplate;
        }

        public async Task<Template> UpdateTemplateAsync(Template template)
        {
            var updatedTemplate = await _templateService.UpdateTemplateAsync(template);
            
            // Update cache
            await _cacheService.SetAsync(CacheKeys.TemplateById(updatedTemplate.TemplateId), updatedTemplate, CacheKeys.TemplateCacheExpiration);
            
            // Invalidate related caches
            await InvalidateUserTemplatesCacheAsync(updatedTemplate.CreatedById);
            
            if (!string.IsNullOrEmpty(updatedTemplate.Category))
            {
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategory(updatedTemplate.Category));
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategoryAndUser(updatedTemplate.Category, updatedTemplate.CreatedById));
            }
            
            _logger.LogDebug("Template {TemplateId} updated and cache refreshed", updatedTemplate.TemplateId);
            return updatedTemplate;
        }

        public async Task<Template> PublishTemplateAsync(Guid templateId)
        {
            var publishedTemplate = await _templateService.PublishTemplateAsync(templateId);
            
            // Update cache
            await _cacheService.SetAsync(CacheKeys.TemplateById(publishedTemplate.TemplateId), publishedTemplate, CacheKeys.TemplateCacheExpiration);
            
            // Invalidate published templates caches
            await _cacheService.RemoveAsync(CacheKeys.PublishedTemplates());
            await _cacheService.RemoveAsync(CacheKeys.PublishedTemplatesByUser(publishedTemplate.CreatedById));
            
            // Invalidate user templates cache
            await InvalidateUserTemplatesCacheAsync(publishedTemplate.CreatedById);
            
            _logger.LogDebug("Template {TemplateId} published and cache refreshed", publishedTemplate.TemplateId);
            return publishedTemplate;
        }

        public async Task<Template> CreateNewVersionAsync(Guid templateId, Template updatedTemplate)
        {
            var newVersion = await _templateService.CreateNewVersionAsync(templateId, updatedTemplate);
            
            // Cache the new version
            await _cacheService.SetAsync(CacheKeys.TemplateById(newVersion.TemplateId), newVersion, CacheKeys.TemplateCacheExpiration);
            
            // Invalidate related caches
            await InvalidateUserTemplatesCacheAsync(newVersion.CreatedById);
            
            if (!string.IsNullOrEmpty(newVersion.Category))
            {
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategory(newVersion.Category));
                await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategoryAndUser(newVersion.Category, newVersion.CreatedById));
            }
            
            _logger.LogDebug("New version {TemplateId} created for template {OriginalTemplateId} and cached", 
                newVersion.TemplateId, templateId);
            return newVersion;
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            _logger.LogInformation("Starting template deletion for template {TemplateId}", templateId);
            
            // Get template directly from the underlying service to avoid caching issues
            var template = await _templateService.GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found in database", templateId);
                return false;
            }
            
            _logger.LogInformation("Template {TemplateId} found in database, proceeding with deletion", templateId);
            
            var result = await _templateService.DeleteTemplateAsync(templateId);
            
            if (result && template != null)
            {
                _logger.LogInformation("Template {TemplateId} deleted from database, invalidating cache", templateId);
                
                // Comprehensive cache invalidation
                await InvalidateAllTemplateCacheAsync(templateId, template);
                
                _logger.LogDebug("Template {TemplateId} deleted and removed from cache", templateId);
            }
            else
            {
                _logger.LogWarning("Template {TemplateId} deletion failed or template was null", templateId);
            }
            
            return result;
        }

        private async Task InvalidateAllTemplateCacheAsync(Guid templateId, Template template)
        {
            try
            {
                _logger.LogInformation("Starting cache invalidation for template {TemplateId}", templateId);
                
                // Remove specific template cache entries
                await _cacheService.RemoveAsync(CacheKeys.TemplateById(templateId));
                _logger.LogDebug("Removed specific template cache entry for {TemplateId}", templateId);
                
                // Remove from cache using pattern matching
                await _cacheService.RemoveByPatternAsync(CacheKeys.TemplatePattern(templateId));
                _logger.LogDebug("Removed template pattern cache entries for {TemplateId}", templateId);
                
                // Also try a more comprehensive pattern to catch any edge cases
                await _cacheService.RemoveByPatternAsync($"*{templateId}*");
                _logger.LogDebug("Removed comprehensive pattern cache entries for {TemplateId}", templateId);
                
                // Invalidate related caches
                await InvalidateUserTemplatesCacheAsync(template.CreatedById);
                _logger.LogDebug("Invalidated user templates cache for user {UserId}", template.CreatedById);
                
                if (!string.IsNullOrEmpty(template.Category))
                {
                    await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategory(template.Category));
                    await _cacheService.RemoveAsync(CacheKeys.TemplatesByCategoryAndUser(template.Category, template.CreatedById));
                    _logger.LogDebug("Invalidated category cache for category {Category}", template.Category);
                }
                
                if (template.IsPublished)
                {
                    await _cacheService.RemoveAsync(CacheKeys.PublishedTemplates());
                    await _cacheService.RemoveAsync(CacheKeys.PublishedTemplatesByUser(template.CreatedById));
                    _logger.LogDebug("Invalidated published templates cache for user {UserId}", template.CreatedById);
                }
                
                // Also invalidate dashboard cache for this template
                await _cacheService.RemoveAsync(CacheKeys.DashboardTemplateStats(templateId));
                _logger.LogDebug("Invalidated dashboard template stats cache for {TemplateId}", templateId);
                
                // Invalidate all assigned templates caches as they might contain this template
                await _cacheService.RemoveByPatternAsync("template:assigned:*");
                _logger.LogDebug("Invalidated all assigned templates cache");
                
                // Invalidate all template-related caches as a final measure
                await _cacheService.RemoveByPatternAsync("template:*");
                _logger.LogDebug("Invalidated all template cache entries");
                
                _logger.LogInformation("All template cache entries invalidated for template {TemplateId}", templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for template {TemplateId}", templateId);
            }
        }

        public async Task<IEnumerable<Template>> GetTemplatesByRoleAsync(string role, Guid userId)
        {
            // This method is less frequently used, so we don't cache it by default
            return await _templateService.GetTemplatesByRoleAsync(role, userId);
        }

        public async Task<bool> TemplateNameExistsAsync(string name, Guid createdById)
        {
            // This is a validation method and should not be cached for data consistency
            return await _templateService.TemplateNameExistsAsync(name, createdById);
        }

        // Helper methods for cache invalidation
        private async Task InvalidateUserTemplatesCacheAsync(Guid userId)
        {
            var cacheKey = CacheKeys.UserTemplatesPattern(userId);
            _logger.LogInformation("Invalidating user templates cache with key: {CacheKey} for user {UserId}", cacheKey, userId);
            
            await _cacheService.RemoveAsync(cacheKey);
            _logger.LogInformation("User templates cache invalidated for user {UserId} using key {CacheKey}", userId, cacheKey);
        }

        public async Task InvalidateTemplateCacheAsync(Guid templateId)
        {
            await _cacheService.RemoveByPatternAsync(CacheKeys.TemplatePattern(templateId));
            _logger.LogDebug("All cache entries for template {TemplateId} invalidated", templateId);
        }

        public async Task InvalidatePublishedTemplatesCacheAsync()
        {
            await _cacheService.RemoveAsync(CacheKeys.PublishedTemplates());
            _logger.LogDebug("Published templates cache invalidated");
        }

        public async Task WarmUpTemplateCacheAsync(Guid templateId)
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId);
            if (template != null)
            {
                await _cacheService.SetAsync(CacheKeys.TemplateById(templateId), template, CacheKeys.TemplateCacheExpiration);
                _logger.LogDebug("Template cache warmed up for template {TemplateId}", templateId);
            }
        }

        public async Task ClearAllTemplateCacheAsync()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync("template:*");
                _logger.LogDebug("All template cache entries cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all template cache entries");
            }
        }

        public async Task<bool> IsTemplateCachedAsync(Guid templateId)
        {
            try
            {
                return await _cacheService.ExistsAsync(CacheKeys.TemplateById(templateId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if template {TemplateId} is cached", templateId);
                return false;
            }
        }
    }
} 