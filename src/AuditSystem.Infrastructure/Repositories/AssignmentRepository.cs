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
    public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByOrganisationAsync(Guid organisationId)
        {
            return await _context.Assignments
                .Where(a => a.OrganisationId == organisationId)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByAuditorAsync(Guid auditorId)
        {
            return await _context.Assignments
                .Where(a => a.AssignedToId == auditorId)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByAssignerAsync(Guid assignerId)
        {
            return await _context.Assignments
                .Where(a => a.AssignedById == assignerId)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByTemplateAsync(Guid templateId)
        {
            return await _context.Assignments
                .Where(a => a.TemplateId == templateId)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(string status)
        {
            return await _context.Assignments
                .Where(a => a.Status == status)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetPendingAssignmentsAsync(Guid organisationId)
        {
            return await _context.Assignments
                .Where(a => a.OrganisationId == organisationId && a.Status == "pending")
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(Guid organisationId)
        {
            var currentDate = DateTime.UtcNow;
            return await _context.Assignments
                .Where(a => a.OrganisationId == organisationId && 
                           a.DueDate.HasValue && 
                           a.DueDate.Value < currentDate &&
                                           a.Status != "fulfilled" &&
                a.Status != "cancelled")
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderBy(a => a.DueDate)
                .ToListAsync();
        }

        public async Task<Assignment> GetAssignmentWithDetailsAsync(Guid assignmentId)
        {
            return await _context.Assignments
                .Where(a => a.AssignmentId == assignmentId)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .Include(a => a.Organisation)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(Guid templateId, Guid auditorId)
        {
            return await _context.Assignments
                .AnyAsync(a => a.TemplateId == templateId && a.AssignedToId == auditorId);
        }

        public new async Task<Assignment> GetByIdAsync(Guid id)
        {
            return await _context.Assignments
                .Where(a => a.AssignmentId == id)
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .FirstOrDefaultAsync();
        }

        public new async Task<IEnumerable<Assignment>> GetAllAsync()
        {
            return await _context.Assignments
                .Include(a => a.Template)
                .Include(a => a.AssignedTo)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
} 