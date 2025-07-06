using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Repositories
{
    public interface ITemplateRepository : IRepository<Template>
    {
        Task<IEnumerable<Template>> GetPublishedTemplatesAsync();
        Task<IEnumerable<Template>> GetPublishedTemplatesAsync(Guid userId);
        Task<IEnumerable<Template>> GetTemplatesByUserIdAsync(Guid userId);
        Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category);
        Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category, Guid userId);
        Task<Template> GetLatestVersionAsync(Guid templateId);
        
        // New methods for role-based access control
        Task<IEnumerable<Template>> GetTemplatesByRoleAsync(string role, Guid userId);
        Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId);
        Task<bool> TemplateNameExistsAsync(string name, Guid createdById);
    }
} 