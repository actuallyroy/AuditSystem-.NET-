using AuditSystem.Services;
using AuditSystem.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;
        private readonly DashboardCacheService _dashboardCacheService;

        public DashboardController(
            IDashboardService dashboardService,
            DashboardCacheService dashboardCacheService)
        {
            _dashboardService = dashboardService;
            _dashboardCacheService = dashboardCacheService;
        }

        /// <summary>
        /// Get dashboard data for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboardData()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var organizationId = GetCurrentUserOrganizationId();

                var dashboardData = await _dashboardService.GetDashboardDataForUserAsync(userId, userRole, organizationId);
                
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve dashboard data", message = ex.Message });
            }
        }

        /// <summary>
        /// Get dashboard data for a specific organization (admin only)
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DashboardResponse>> GetDashboardDataForOrganization(Guid organizationId)
        {
            try
            {
                var dashboardData = await _dashboardService.GetDashboardDataAsync(organizationId);
                
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve dashboard data", message = ex.Message });
            }
        }

        /// <summary>
        /// Clear dashboard cache for the current user's organization
        /// </summary>
        [HttpDelete("cache/clear")]
        public async Task<ActionResult> ClearDashboardCache()
        {
            try
            {
                var organizationId = GetCurrentUserOrganizationId();
                if (organizationId.HasValue)
                {
                    await _dashboardCacheService.InvalidateOrganizationDashboardCacheAsync(organizationId.Value);
                }
                
                return Ok(new { message = "Dashboard cache cleared successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to clear dashboard cache", message = ex.Message });
            }
        }

        /// <summary>
        /// Clear dashboard cache for a specific organization (admin only)
        /// </summary>
        [HttpDelete("cache/clear/{organizationId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> ClearDashboardCacheForOrganization(Guid organizationId)
        {
            try
            {
                await _dashboardCacheService.InvalidateOrganizationDashboardCacheAsync(organizationId);
                
                return Ok(new { message = $"Dashboard cache cleared successfully for organization {organizationId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to clear dashboard cache", message = ex.Message });
            }
        }

        /// <summary>
        /// Get dashboard cache health status
        /// </summary>
        [HttpGet("cache/health")]
        public async Task<ActionResult> GetDashboardCacheHealth()
        {
            try
            {
                var isHealthy = await _dashboardCacheService.IsHealthyAsync();
                
                return Ok(new 
                { 
                    isHealthy = isHealthy,
                    message = isHealthy ? "Dashboard cache is healthy" : "Dashboard cache is not responding",
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    isHealthy = false,
                    error = "Dashboard cache health check failed",
                    message = ex.Message,
                    checkedAt = DateTime.UtcNow
                });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("User ID not found in claims");
            }
            return userId;
        }

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? "auditor";
        }

        private Guid? GetCurrentUserOrganizationId()
        {
            var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var orgId))
            {
                return null;
            }
            return orgId;
        }
    }
} 