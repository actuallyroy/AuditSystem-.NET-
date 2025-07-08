using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IOrganisationRepository _organisationRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationTemplateRepository templateRepository,
            IUserRepository userRepository,
            IAuditRepository auditRepository,
            IOrganisationRepository organisationRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _templateRepository = templateRepository;
            _userRepository = userRepository;
            _auditRepository = auditRepository;
            _organisationRepository = organisationRepository;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.NotificationId = Guid.NewGuid();
                notification.CreatedAt = DateTime.UtcNow;
                notification.Status = "pending";

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();

                _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                    notification.NotificationId, notification.UserId);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for user {UserId}", notification.UserId);
                throw;
            }
        }

        public async Task<Notification> CreateNotificationFromTemplateAsync(string templateName, Dictionary<string, object> placeholders, Guid? userId = null, Guid? organisationId = null)
        {
            try
            {
                var template = await _templateRepository.GetByNameAsync(templateName);
                if (template == null)
                {
                    throw new ArgumentException($"Template '{templateName}' not found");
                }

                var title = template.Subject;
                var message = template.Body;

                // Replace placeholders in title and message
                foreach (var placeholder in placeholders)
                {
                    title = title.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                    message = message.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                }

                var notification = new Notification
                {
                    UserId = userId,
                    OrganisationId = organisationId,
                    Type = template.Type,
                    Title = title,
                    Message = message,
                    Channel = template.Channel,
                    Priority = "medium",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                return await CreateNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification from template {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepository.GetNotificationsByUserAsync(userId, page, pageSize);
        }

        public async Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepository.GetNotificationsByOrganisationAsync(organisationId, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountByUserAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                return await _notificationRepository.MarkAsReadAsync(notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                return await _notificationRepository.MarkAllAsReadByUserAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                return await _notificationRepository.DeleteNotificationAsync(notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<bool> SendAuditAssignmentNotificationAsync(Guid auditId, Guid userId, Guid organisationId)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" },
                    { "due_date", audit.Assignment?.DueDate?.ToString("MMM dd, yyyy") ?? "TBD" }
                };

                await CreateNotificationFromTemplateAsync("assignment_notification", placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit assignment notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendAuditCompletedNotificationAsync(Guid auditId, Guid userId, Guid organisationId)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" }
                };

                await CreateNotificationFromTemplateAsync("audit_completed_notification", placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit completed notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendAuditReviewedNotificationAsync(Guid auditId, Guid userId, Guid organisationId, string status, decimal? score = null)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                string templateName = status.ToLower() == "approved" ? "audit_approved_notification" : "audit_rejected_notification";
                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" }
                };

                if (status.ToLower() == "approved" && score.HasValue)
                {
                    placeholders.Add("score", score.Value.ToString("F1"));
                    placeholders.Add("critical_issues", audit.CriticalIssues.ToString());
                }
                else if (status.ToLower() == "rejected")
                {
                    placeholders.Add("reason", audit.ManagerNotes ?? "No specific reason provided");
                }

                await CreateNotificationFromTemplateAsync(templateName, placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit reviewed notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendSystemAlertAsync(string title, string message, Guid? organisationId = null, string priority = "medium")
        {
            try
            {
                var placeholders = new Dictionary<string, object>
                {
                    { "message", message }
                };

                await CreateNotificationFromTemplateAsync("system_notification", placeholders, null, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system alert: {Title}", title);
                return false;
            }
        }

        public async Task<bool> SendBulkNotificationAsync(string title, string message, List<Guid> userIds, Guid? organisationId = null)
        {
            try
            {
                var notifications = new List<Notification>();
                foreach (var userId in userIds)
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        OrganisationId = organisationId,
                        Type = "system_alert",
                        Title = title,
                        Message = message,
                        Channel = "in_app",
                        Priority = "medium",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    notifications.Add(notification);
                }

                await _notificationRepository.AddRangeAsync(notifications);
                await _notificationRepository.SaveChangesAsync();

                _logger.LogInformation("Sent bulk notification to {Count} users", userIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification to {Count} users", userIds.Count);
                return false;
            }
        }

        public async Task<bool> ProcessNotificationDeliveryAsync()
        {
            try
            {
                // This method would handle the actual delivery of notifications
                // For now, we'll just mark pending notifications as sent
                var pendingNotifications = await _notificationRepository.FindAsync(n => n.Status == "pending");
                
                foreach (var notification in pendingNotifications)
                {
                    notification.Status = "sent";
                    notification.SentAt = DateTime.UtcNow;
                    _notificationRepository.Update(notification);
                }

                await _notificationRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification delivery");
                return false;
            }
        }

        public async Task<bool> CleanupExpiredNotificationsAsync()
        {
            try
            {
                return await _notificationRepository.DeleteExpiredNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired notifications");
                return false;
            }
        }
    }
} 