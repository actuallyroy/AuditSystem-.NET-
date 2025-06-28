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
    public class TemplateRepository : Repository<Template>, ITemplateRepository
    {
        public TemplateRepository(AuditSystemDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync()
        {
            return await _context.Templates
                .Where(t => t.IsPublished)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Template>> GetTemplatesByUserIdAsync(Guid userId)
        {
            return await _context.Templates
                .Where(t => t.CreatedById == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category)
        {
            return await _context.Templates
                .Where(t => t.Category == category && t.IsPublished)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Template> GetLatestVersionAsync(Guid templateId)
        {
            return await _context.Templates
                .Where(t => t.TemplateId == templateId)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync();
        }
        
        public async Task<IEnumerable<Template>> GetTemplatesByRoleAsync(string role, Guid userId)
        {
            // Admin can see all templates
            if (role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Manager can see their own templates and all published templates
            if (role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .Where(t => t.CreatedById == userId || t.IsPublished)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Supervisor can only see published templates
            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .Where(t => t.IsPublished)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Auditor can only see templates assigned to them
            return await GetAssignedTemplatesAsync(userId);
        }

        public async Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId)
        {
            // Get templates from assignments where user is assigned
            return await _context.Assignments
                .Where(a => a.AssignedToId == auditorId)
                .Include(a => a.Template)
                .Select(a => a.Template)
                .Distinct()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
} 