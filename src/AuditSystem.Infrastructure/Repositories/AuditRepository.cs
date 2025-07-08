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
    public class AuditRepository : Repository<Audit>, IAuditRepository
    {
        public AuditRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public override async Task<Audit> GetByIdAsync(Guid id)
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .FirstOrDefaultAsync(a => a.AuditId == id);
        }

        public async Task<IEnumerable<Audit>> GetAuditsByAuditorIdAsync(Guid auditorId)
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.AuditorId == auditorId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetAuditsByOrganisationIdAsync(Guid organisationId)
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.OrganisationId == organisationId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetAuditsByTemplateIdAsync(Guid templateId)
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.TemplateId == templateId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetAllAuditsWithNavigationPropertiesAsync()
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetAuditsPendingSyncAsync()
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.SyncFlag == true)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetFlaggedAuditsAsync()
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.IsFlagged == true)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetAuditsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Audits
                .Include(a => a.Template)
                .Include(a => a.Auditor)
                .Include(a => a.Organisation)
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
} 