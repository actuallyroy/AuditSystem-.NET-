using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<IEnumerable<Notification>> CreateBulkNotificationsAsync(IEnumerable<Notification> notifications);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool includeRead = false, int limit = 50);
        Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, bool includeRead = false, int limit = 50);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAsReadAsync(Guid userId, IEnumerable<Guid> notificationIds);
        Task DeleteNotificationAsync(Guid notificationId);
        Task DeleteExpiredNotificationsAsync();
        
        // Notification creation helpers
        Task<Notification> CreateAssignmentNotificationAsync(Guid userId, Guid assignmentId, string storeName);
        Task<Notification> CreateAuditCompletedNotificationAsync(Guid auditorId, Guid auditId, string storeName);
        Task<Notification> CreateAuditApprovedNotificationAsync(Guid auditorId, Guid auditId, string storeName);
        Task<Notification> CreateAuditRejectedNotificationAsync(Guid auditorId, Guid auditId, string storeName, string reason);
        Task<Notification> CreateSystemNotificationAsync(Guid? userId, Guid? organisationId, string title, string message, string priority = "medium");
        
        // Template management
        Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template);
        Task<NotificationTemplate> UpdateTemplateAsync(NotificationTemplate template);
        Task<NotificationTemplate> GetTemplateByNameAsync(string name);
        Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync();
    }
} 