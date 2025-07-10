using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetNotificationsByUserAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Notification>> GetUnreadNotificationsByUserAsync(Guid userId);
        Task<IEnumerable<Notification>> GetNotificationsByOrganisationAsync(Guid organisationId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountByUserAsync(Guid userId);
        Task<int> GetUnreadCountByOrganisationAsync(Guid organisationId);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task<bool> MarkAllAsReadByUserAsync(Guid userId);
        Task<bool> DeleteNotificationAsync(Guid notificationId);
        Task<bool> DeleteExpiredNotificationsAsync();
        Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type, Guid? userId = null, Guid? organisationId = null);
    }
} 