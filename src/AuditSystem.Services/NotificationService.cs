using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IQueueService _queueService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IQueueService queueService,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _queueService = queueService;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.CreatedAt = DateTime.UtcNow;
                notification.Status = "pending";
                notification.RetryCount = 0;

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();

                // Publish to queue for processing
                await PublishToQueueAsync(notification);

                _logger.LogInformation("Created notification {NotificationId} of type {Type} for user {UserId}", 
                    notification.NotificationId, notification.Type, notification.UserId);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", notification.UserId);
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> CreateBulkNotificationsAsync(IEnumerable<Notification> notifications)
        {
            var createdNotifications = new List<Notification>();
            
            foreach (var notification in notifications)
            {
                try
                {
                    var created = await CreateNotificationAsync(notification);
                    createdNotifications.Add(created);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating bulk notification for user {UserId}", notification.UserId);
                }
            }

            return createdNotifications;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool includeRead = false, int limit = 50)
        {
            return await _notificationRepository.GetUserNotificationsAsync(userId, includeRead, limit);
        }

        public async Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, bool includeRead = false, int limit = 50)
        {
            return await _notificationRepository.GetOrganisationNotificationsAsync(organisationId, includeRead, limit);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task MarkAsReadAsync(Guid userId, IEnumerable<Guid> notificationIds)
        {
            await _notificationRepository.MarkAsReadAsync(userId, notificationIds);
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            await _notificationRepository.DeleteAsync(notificationId);
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task DeleteExpiredNotificationsAsync()
        {
            var expiredNotifications = await _notificationRepository.GetAllAsync();
            var toDelete = expiredNotifications.Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTime.UtcNow);

            foreach (var notification in toDelete)
            {
                await _notificationRepository.DeleteAsync(notification.NotificationId);
            }

            await _notificationRepository.SaveChangesAsync();
        }

        // Notification creation helpers
        public async Task<Notification> CreateAssignmentNotificationAsync(Guid userId, Guid assignmentId, string storeName)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = "assignment",
                Title = "New Audit Assignment",
                Message = $"You have been assigned a new audit for {storeName}",
                Priority = "medium",
                Channel = "in_app",
                Metadata = new Dictionary<string, object>
                {
                    ["assignment_id"] = assignmentId,
                    ["store_name"] = storeName
                }
            };

            return await CreateNotificationAsync(notification);
        }

        public async Task<Notification> CreateAuditCompletedNotificationAsync(Guid auditorId, Guid auditId, string storeName)
        {
            var notification = new Notification
            {
                UserId = auditorId,
                Type = "audit_completed",
                Title = "Audit Completed",
                Message = $"Your audit for {storeName} has been completed and submitted for review",
                Priority = "medium",
                Channel = "in_app",
                Metadata = new Dictionary<string, object>
                {
                    ["audit_id"] = auditId,
                    ["store_name"] = storeName
                }
            };

            return await CreateNotificationAsync(notification);
        }

        public async Task<Notification> CreateAuditApprovedNotificationAsync(Guid auditorId, Guid auditId, string storeName)
        {
            var notification = new Notification
            {
                UserId = auditorId,
                Type = "audit_approved",
                Title = "Audit Approved",
                Message = $"Your audit for {storeName} has been approved",
                Priority = "low",
                Channel = "in_app",
                Metadata = new Dictionary<string, object>
                {
                    ["audit_id"] = auditId,
                    ["store_name"] = storeName
                }
            };

            return await CreateNotificationAsync(notification);
        }

        public async Task<Notification> CreateAuditRejectedNotificationAsync(Guid auditorId, Guid auditId, string storeName, string reason)
        {
            var notification = new Notification
            {
                UserId = auditorId,
                Type = "audit_rejected",
                Title = "Audit Rejected",
                Message = $"Your audit for {storeName} has been rejected. Reason: {reason}",
                Priority = "high",
                Channel = "in_app",
                Metadata = new Dictionary<string, object>
                {
                    ["audit_id"] = auditId,
                    ["store_name"] = storeName,
                    ["reason"] = reason
                }
            };

            return await CreateNotificationAsync(notification);
        }

        public async Task<Notification> CreateSystemNotificationAsync(Guid? userId, Guid? organisationId, string title, string message, string priority = "medium")
        {
            var notification = new Notification
            {
                UserId = userId,
                OrganisationId = organisationId,
                Type = "system",
                Title = title,
                Message = message,
                Priority = priority,
                Channel = "in_app"
            };

            return await CreateNotificationAsync(notification);
        }

        // Template management
        public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.IsActive = true;
            
            // This would need to be implemented in the repository
            // For now, we'll throw NotImplementedException
            throw new NotImplementedException("Template creation not yet implemented");
        }

        public async Task<NotificationTemplate> UpdateTemplateAsync(NotificationTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            
            // This would need to be implemented in the repository
            // For now, we'll throw NotImplementedException
            throw new NotImplementedException("Template update not yet implemented");
        }

        public async Task<NotificationTemplate> GetTemplateByNameAsync(string name)
        {
            return await _notificationRepository.GetTemplateByNameAsync(name);
        }

        public async Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync()
        {
            return await _notificationRepository.GetActiveTemplatesAsync();
        }

        private async Task PublishToQueueAsync(Notification notification)
        {
            try
            {
                var message = new NotificationMessage
                {
                    Type = notification.Type,
                    Channel = notification.Channel,
                    UserId = notification.UserId,
                    OrganisationId = notification.OrganisationId,
                    Subject = notification.Title,
                    Body = notification.Message,
                    Priority = notification.Priority,
                    Metadata = notification.Metadata,
                    Status = notification.Status
                };

                await _queueService.PublishNotificationAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing notification {NotificationId} to queue", notification.NotificationId);
                
                // Update notification status to failed
                notification.Status = "failed";
                notification.ErrorMessage = ex.Message;
                await _notificationRepository.UpdateAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }
    }
} 