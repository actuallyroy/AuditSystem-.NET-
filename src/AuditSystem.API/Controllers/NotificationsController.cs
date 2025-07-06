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
    [ApiController]
    [Route("api/[controller]")]
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
        /// Get user's notifications
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(
            [FromQuery] bool includeRead = false,
            [FromQuery] int limit = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead, limit);
                
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notifications for user {UserId}", GetCurrentUserId());
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        /// <summary>
        /// Get organization notifications (for managers/admins)
        /// </summary>
        [HttpGet("organisation")]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetOrganisationNotifications(
            [FromQuery] bool includeRead = false,
            [FromQuery] int limit = 50)
        {
            try
            {
                var organisationId = GetCurrentUserOrganisationId();
                if (!organisationId.HasValue)
                {
                    return BadRequest("User is not associated with an organization");
                }

                var notifications = await _notificationService.GetOrganisationNotificationsAsync(organisationId.Value, includeRead, limit);
                
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organisation notifications for organisation {OrganisationId}", GetCurrentUserOrganisationId());
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", GetCurrentUserId());
                return StatusCode(500, "An error occurred while retrieving unread count");
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPut("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Verify the notification belongs to the current user
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, true, 1000);
                var notification = notifications.FirstOrDefault(n => n.NotificationId == notificationId);
                
                if (notification == null)
                {
                    return NotFound("Notification not found or access denied");
                }

                await _notificationService.MarkAsReadAsync(notificationId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, GetCurrentUserId());
                return StatusCode(500, "An error occurred while marking notification as read");
            }
        }

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        [HttpPut("mark-read")]
        public async Task<ActionResult> MarkMultipleAsRead([FromBody] List<Guid> notificationIds)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAsReadAsync(userId, notificationIds);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple notifications as read for user {UserId}", GetCurrentUserId());
                return StatusCode(500, "An error occurred while marking notifications as read");
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
                
                // Verify the notification belongs to the current user
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, true, 1000);
                var notification = notifications.FirstOrDefault(n => n.NotificationId == notificationId);
                
                if (notification == null)
                {
                    return NotFound("Notification not found or access denied");
                }

                await _notificationService.DeleteNotificationAsync(notificationId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, GetCurrentUserId());
                return StatusCode(500, "An error occurred while deleting notification");
            }
        }

        /// <summary>
        /// Create a system notification (admin only)
        /// </summary>
        [HttpPost("system")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Notification>> CreateSystemNotification([FromBody] CreateSystemNotificationRequest request)
        {
            try
            {
                var notification = await _notificationService.CreateSystemNotificationAsync(
                    request.UserId,
                    request.OrganisationId,
                    request.Title,
                    request.Message,
                    request.Priority);
                
                return CreatedAtAction(nameof(GetUserNotifications), new { id = notification.NotificationId }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
                return StatusCode(500, "An error occurred while creating system notification");
            }
        }

        /// <summary>
        /// Get notification templates (admin only)
        /// </summary>
        [HttpGet("templates")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<NotificationTemplate>>> GetTemplates()
        {
            try
            {
                var templates = await _notificationService.GetActiveTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification templates");
                return StatusCode(500, "An error occurred while retrieving templates");
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Invalid user ID in claims");
            }
            return userId;
        }

        private Guid? GetCurrentUserOrganisationId()
        {
            var organisationIdClaim = User.FindFirst("OrganisationId")?.Value;
            if (string.IsNullOrEmpty(organisationIdClaim) || !Guid.TryParse(organisationIdClaim, out var organisationId))
            {
                return null;
            }
            return organisationId;
        }
    }

    public class CreateSystemNotificationRequest
    {
        public Guid? UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Priority { get; set; } = "medium";
    }
} 