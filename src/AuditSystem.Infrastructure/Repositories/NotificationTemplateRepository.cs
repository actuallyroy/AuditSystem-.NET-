using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class NotificationTemplateRepository : Repository<NotificationTemplate>, INotificationTemplateRepository
    {
        public NotificationTemplateRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<NotificationTemplate?> GetByNameAsync(string name)
        {
            return await _context.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.Name == name && t.IsActive);
        }

        public async Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(string type)
        {
            return await _context.Set<NotificationTemplate>()
                .Where(t => t.Type == type && t.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.Set<NotificationTemplate>()
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<NotificationTemplate>> GetByChannelAsync(string channel)
        {
            return await _context.Set<NotificationTemplate>()
                .Where(t => t.Channel == channel && t.IsActive)
                .ToListAsync();
        }
    }
} 