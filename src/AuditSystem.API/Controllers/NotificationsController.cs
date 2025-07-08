using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
                
                return Ok(new
                {
                    notifications,
                    page,
                    pageSize,
                    totalCount = notifications.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notifications for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { error = "Failed to retrieve notifications", message = ex.Message });
            }
        }

        /// <summary>
        /// Get unread notification count for the current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { error = "Failed to retrieve unread count", message = ex.Message });
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _notificationService.MarkAsReadAsync(notificationId, userId);
                
                if (!success)
                {
                    return NotFound(new { error = "Notification not found or access denied" });
                }

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", 
                    notificationId, GetCurrentUserId());
                return StatusCode(500, new { error = "Failed to mark notification as read", message = ex.Message });
            }
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _notificationService.MarkAllAsReadAsync(userId);
                
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to mark notifications as read" });
                }

                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { error = "Failed to mark notifications as read", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{notificationId}")]
        public async Task<ActionResult> DeleteNotification(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _notificationService.DeleteNotificationAsync(notificationId, userId);
                
                if (!success)
                {
                    return NotFound(new { error = "Notification not found or access denied" });
                }

                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for user {UserId}", 
                    notificationId, GetCurrentUserId());
                return StatusCode(500, new { error = "Failed to delete notification", message = ex.Message });
            }
        }

        /// <summary>
        /// Send a system alert (Admin/Manager only)
        /// </summary>
        [HttpPost("system-alert")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> SendSystemAlert([FromBody] SystemAlertRequest request)
        {
            try
            {
                var organisationId = GetCurrentUserOrganizationId();
                var success = await _notificationService.SendSystemAlertAsync(
                    request.Title, 
                    request.Message, 
                    organisationId, 
                    request.Priority);

                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to send system alert" });
                }

                return Ok(new { message = "System alert sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system alert: {Title}", request.Title);
                return StatusCode(500, new { error = "Failed to send system alert", message = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk notification (Admin only)
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> SendBulkNotification([FromBody] BulkNotificationRequest request)
        {
            try
            {
                var organisationId = GetCurrentUserOrganizationId();
                var success = await _notificationService.SendBulkNotificationAsync(
                    request.Title, 
                    request.Message, 
                    request.UserIds, 
                    organisationId);

                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to send bulk notification" });
                }

                return Ok(new { message = $"Bulk notification sent to {request.UserIds.Count} users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification to {Count} users", request.UserIds.Count);
                return StatusCode(500, new { error = "Failed to send bulk notification", message = ex.Message });
            }
        }

        /// <summary>
        /// Get organisation notifications (Admin/Manager only)
        /// </summary>
        [HttpGet("organisation")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetOrganisationNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var organisationId = GetCurrentUserOrganizationId();
                if (!organisationId.HasValue)
                {
                    return BadRequest(new { error = "User not associated with an organisation" });
                }

                var notifications = await _notificationService.GetOrganisationNotificationsAsync(organisationId.Value, page, pageSize);
                
                return Ok(new
                {
                    notifications,
                    page,
                    pageSize,
                    totalCount = notifications.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get organisation notifications for organisation {OrganisationId}", 
                    GetCurrentUserOrganizationId());
                return StatusCode(500, new { error = "Failed to retrieve organisation notifications", message = ex.Message });
            }
        }

        // Request models
        public class SystemAlertRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Priority { get; set; } = "medium";
        }

        public class BulkNotificationRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public List<Guid> UserIds { get; set; } = new();
        }

        // Add helper methods for user and organization context
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("User ID not found in claims");
            }
            return userId;
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

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? "auditor";
        }
    }
} 