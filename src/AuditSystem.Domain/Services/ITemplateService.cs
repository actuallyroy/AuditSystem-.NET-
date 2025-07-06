using AuditSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface ITemplateService
    {
        Task<Template> GetTemplateByIdAsync(Guid templateId);
        Task<IEnumerable<Template>> GetAllTemplatesAsync();
        Task<IEnumerable<Template>> GetPublishedTemplatesAsync();
        Task<IEnumerable<Template>> GetPublishedTemplatesAsync(Guid userId);
        Task<IEnumerable<Template>> GetTemplatesByUserAsync(Guid userId);
        Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category);
        Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category, Guid userId);
        Task<Template> CreateTemplateAsync(Template template);
        Task<Template> UpdateTemplateAsync(Template template);
        Task<Template> PublishTemplateAsync(Guid templateId);
        Task<Template> CreateNewVersionAsync(Guid templateId, Template updatedTemplate);
        Task<bool> DeleteTemplateAsync(Guid templateId);
        
        // New methods for role-based access control
        Task<IEnumerable<Template>> GetTemplatesByRoleAsync(string role, Guid userId);
        Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId);
        Task<bool> TemplateNameExistsAsync(string name, Guid createdById);
    }
} 