using AuditSystem.Domain.Services;
using AuditSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuditSystem.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CacheResponseAttribute : ActionFilterAttribute
    {
        private readonly int _durationInMinutes;
        private readonly bool _varyByUser;
        private readonly bool _varyByOrganization;

        public CacheResponseAttribute(int durationInMinutes = 5, bool varyByUser = false, bool varyByOrganization = false)
        {
            _durationInMinutes = durationInMinutes;
            _varyByUser = varyByUser;
            _varyByOrganization = varyByOrganization;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cacheService = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
            
            // Generate cache key
            var cacheKey = GenerateCacheKey(context);
            
            // Try to get from cache
            var cachedResponse = await cacheService.GetAsync<CachedApiResponse>(cacheKey);
            if (cachedResponse != null && cachedResponse.ExpiresAt > DateTime.UtcNow)
            {
                context.Result = new JsonResult(cachedResponse.Data)
                {
                    StatusCode = cachedResponse.StatusCode
                };
                return;
            }

            // Execute action
            var executedContext = await next();

            // Cache the response if successful
            if (executedContext.Result is JsonResult jsonResult && 
                jsonResult.StatusCode >= 200 && jsonResult.StatusCode < 300)
            {
                var responseToCache = new CachedApiResponse
                {
                    Data = jsonResult.Value,
                    StatusCode = jsonResult.StatusCode ?? 200,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_durationInMinutes)
                };

                await cacheService.SetAsync(cacheKey, responseToCache, TimeSpan.FromMinutes(_durationInMinutes));
            }
        }

        private string GenerateCacheKey(ActionExecutingContext context)
        {
            var keyBuilder = new StringBuilder();
            
            // Controller and action
            var controllerName = context.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ActionDescriptor.RouteValues["action"];
            keyBuilder.Append($"{controllerName}:{actionName}");

            // Parameters
            var parameters = context.ActionArguments
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")
                .ToArray();
            
            if (parameters.Any())
            {
                keyBuilder.Append($":{string.Join("&", parameters)}");
            }

            // User-specific caching
            if (_varyByUser)
            {
                var userId = context.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    keyBuilder.Append($":user:{userId}");
                }
            }

            // Organization-specific caching
            if (_varyByOrganization)
            {
                var organizationId = context.HttpContext.User?.FindFirst("organisation_id")?.Value;
                if (!string.IsNullOrEmpty(organizationId))
                {
                    keyBuilder.Append($":org:{organizationId}");
                }
            }

            return CacheKeys.ApiResponse(controllerName, actionName, keyBuilder.ToString());
        }
    }

    public class CachedApiResponse
    {
        public object? Data { get; set; }
        public int StatusCode { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
} 