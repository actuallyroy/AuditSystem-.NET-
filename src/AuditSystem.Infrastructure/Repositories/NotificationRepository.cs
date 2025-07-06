using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private readonly AuditSystemDbContext _context;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(AuditSystemDbContext context, ILogger<NotificationRepository> logger) 
            : base(context)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool includeRead = false, int limit = 50)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, bool includeRead = false, int limit = 50)
        {
            var query = _context.Notifications.Where(n => n.OrganisationId == organisationId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync()
        {
            return await _context.Notifications
                .Where(n => n.Status == "pending")
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetFailedNotificationsAsync(int maxRetries = 3)
        {
            return await _context.Notifications
                .Where(n => n.Status == "failed" && n.RetryCount < maxRetries)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsReadAsync(Guid userId, IEnumerable<Guid> notificationIds)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && notificationIds.Contains(n.NotificationId))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Guid notificationId, string status, string? errorMessage = null)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = status;
                notification.ErrorMessage = errorMessage ?? string.Empty;
                
                if (status == "sent")
                {
                    notification.SentAt = DateTime.UtcNow;
                }
                else if (status == "delivered")
                {
                    notification.DeliveredAt = DateTime.UtcNow;
                }
                else if (status == "failed")
                {
                    notification.RetryCount++;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.NotificationTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<NotificationTemplate> GetTemplateByNameAsync(string name)
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == name && t.IsActive);
        }

        public async Task DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }
    }
} 