using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AuditSystem.Infrastructure.Repositories
{
    public class OrganisationRepository : Repository<Organisation>, IOrganisationRepository
    {
        public OrganisationRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public override async Task<Organisation> GetByIdAsync(Guid id)
        {
            return await _context.Organisations
                .AsSplitQuery() // Use split queries to handle multiple collections efficiently
                .Include(o => o.Users)
                    .ThenInclude(u => u.CreatedTemplates)
                .Include(o => o.Users)
                    .ThenInclude(u => u.AssignedByAssignments)
                        .ThenInclude(a => a.Template)
                .Include(o => o.Users)
                    .ThenInclude(u => u.AssignedByAssignments)
                        .ThenInclude(a => a.AssignedTo)
                .Include(o => o.Users)
                    .ThenInclude(u => u.AssignedToAssignments)
                        .ThenInclude(a => a.Template)
                .Include(o => o.Users)
                    .ThenInclude(u => u.AssignedToAssignments)
                        .ThenInclude(a => a.AssignedBy)
                .Include(o => o.Users)
                    .ThenInclude(u => u.Audits)
                        .ThenInclude(a => a.Template)
                .Include(o => o.Users)
                    .ThenInclude(u => u.Reports)
                .Include(o => o.Users)
                    .ThenInclude(u => u.Logs)
                .Include(o => o.Assignments)
                    .ThenInclude(a => a.Template)
                .Include(o => o.Assignments)
                    .ThenInclude(a => a.AssignedTo)
                .Include(o => o.Assignments)
                    .ThenInclude(a => a.AssignedBy)
                .Include(o => o.Audits)
                    .ThenInclude(a => a.Template)
                .Include(o => o.Audits)
                    .ThenInclude(a => a.Auditor)
                .FirstOrDefaultAsync(o => o.OrganisationId == id);
        }

        public async Task<Organisation> GetByNameAsync(string name)
        {
            return await _context.Organisations
                .FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
        }
    }
} 