using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<Notification> CreateNotificationFromTemplateAsync(string templateName, Dictionary<string, object> placeholders, Guid? userId = null, Guid? organisationId = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId);
        Task<bool> SendAuditAssignmentNotificationAsync(Guid auditId, Guid userId, Guid organisationId);
        Task<bool> SendAssignmentNotificationAsync(Guid assignmentId, Guid userId, Guid organisationId);
        Task<bool> SendAuditCompletedNotificationAsync(Guid auditId, Guid userId, Guid organisationId);
        Task<bool> SendAuditReviewedNotificationAsync(Guid auditId, Guid userId, Guid organisationId, string status, decimal? score = null);
        Task<bool> SendSystemAlertAsync(string title, string message, Guid? organisationId = null, string priority = "medium");
        Task<bool> SendBulkNotificationAsync(string title, string message, List<Guid> userIds, Guid? organisationId = null);
        Task<bool> ProcessNotificationDeliveryAsync();
        Task<bool> CleanupExpiredNotificationsAsync();
        
        // New methods for notification broadcasting
        Task<IEnumerable<Notification>> GetReadyToSendNotificationsAsync();
        Task<bool> MarkNotificationAsSentAsync(Guid notificationId);
        Task<bool> MarkNotificationAsFailedAsync(Guid notificationId, string errorMessage);
    }
} 