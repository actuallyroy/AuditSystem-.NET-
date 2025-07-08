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

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync(Guid userId)
        {
            // Get user details to check organization
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();
                
            if (user == null)
                return new List<Template>(); // Return empty list if user not found
            
            // Return published templates from user's organization
            return await _context.Templates
                .Where(t => t.IsPublished && t.CreatedBy.OrganisationId == user.OrganisationId)
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

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category, Guid userId)
        {
            // Get user details to check organization
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();
                
            if (user == null)
                return new List<Template>(); // Return empty list if user not found
            
            // Return templates by category from user's organization
            return await _context.Templates
                .Where(t => t.Category == category && 
                           t.IsPublished && 
                           t.CreatedBy.OrganisationId == user.OrganisationId)
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
            // Get user details to check organization
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();
                
            if (user == null)
                return new List<Template>(); // Return empty list if user not found
            
            // Admin can see all templates
            if (role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Manager can see their own templates and published templates from their organization
            if (role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .Where(t => (t.CreatedById == userId || t.IsPublished) && 
                               (t.CreatedBy.OrganisationId == user.OrganisationId || t.IsPublished))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Supervisor can only see published templates from their organization
            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Templates
                    .Where(t => t.IsPublished && t.CreatedBy.OrganisationId == user.OrganisationId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            
            // Auditor can only see templates assigned to them
            return await GetAssignedTemplatesAsync(userId);
        }

        public async Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId)
        {
            // Get user details to check organization
            var user = await _context.Users
                .Where(u => u.UserId == auditorId)
                .FirstOrDefaultAsync();
                
            if (user == null)
                return new List<Template>(); // Return empty list if user not found
            
            // Get templates from assignments where user is assigned, only from their organization
            return await _context.Assignments
                .Where(a => a.AssignedToId == auditorId && 
                           a.OrganisationId == user.OrganisationId)
                .Include(a => a.Template)
                .Select(a => a.Template)
                .Distinct()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        
        public async Task<bool> TemplateNameExistsAsync(string name, Guid createdById)
        {
            return await _context.Templates
                .AnyAsync(t => t.Name.ToLower() == name.ToLower() && 
                              t.CreatedById == createdById);
        }
    }
} 