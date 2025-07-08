using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByOrganisationAsync(Guid organisationId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            return await _context.Set<Notification>()
                .Where(n => n.OrganisationId == organisationId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountByUserAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<int> GetUnreadCountByOrganisationAsync(Guid organisationId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.OrganisationId == organisationId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Set<Notification>().FindAsync(notificationId);
            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _context.Set<Notification>().Update(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadByUserAsync(Guid userId)
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            _context.Set<Notification>().UpdateRange(notifications);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _context.Set<Notification>().FindAsync(notificationId);
            if (notification == null)
                return false;

            _context.Set<Notification>().Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteExpiredNotificationsAsync()
        {
            var expiredNotifications = await _context.Set<Notification>()
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.Set<Notification>().RemoveRange(expiredNotifications);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type, Guid? userId = null, Guid? organisationId = null)
        {
            var query = _context.Set<Notification>().Where(n => n.Type == type);

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId);

            if (organisationId.HasValue)
                query = query.Where(n => n.OrganisationId == organisationId);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
} 