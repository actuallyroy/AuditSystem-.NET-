using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
    {
        Task<NotificationTemplate?> GetByNameAsync(string name);
        Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(string type);
        Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync();
        Task<IEnumerable<NotificationTemplate>> GetByChannelAsync(string channel);
    }
} 