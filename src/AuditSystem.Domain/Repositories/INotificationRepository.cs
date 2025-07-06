using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool includeRead = false, int limit = 50);
        Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, bool includeRead = false, int limit = 50);
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync();
        Task<IEnumerable<Notification>> GetFailedNotificationsAsync(int maxRetries = 3);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAsReadAsync(Guid userId, IEnumerable<Guid> notificationIds);
        Task UpdateStatusAsync(Guid notificationId, string status, string? errorMessage = null);
        Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync();
        Task<NotificationTemplate> GetTemplateByNameAsync(string name);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Notification notification);
    }
} 