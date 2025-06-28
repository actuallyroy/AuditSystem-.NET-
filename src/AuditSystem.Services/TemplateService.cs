using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;

        public TemplateService(ITemplateRepository templateRepository)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        public async Task<Template> GetTemplateByIdAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
                
            return await _templateRepository.GetByIdAsync(templateId);
        }

        public async Task<IEnumerable<Template>> GetAllTemplatesAsync()
        {
            return await _templateRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync()
        {
            return await _templateRepository.GetPublishedTemplatesAsync();
        }

        public async Task<IEnumerable<Template>> GetTemplatesByUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
                
            return await _templateRepository.GetTemplatesByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));
                
            return await _templateRepository.GetTemplatesByCategoryAsync(category);
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            ValidateTemplate(template);

            // Set initial values for new template
            template.TemplateId = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            template.Version = 1;
            template.IsPublished = false;

            await _templateRepository.AddAsync(template);
            await _templateRepository.SaveChangesAsync();

            return template;
        }

        public async Task<Template> UpdateTemplateAsync(Template template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
                
            ValidateTemplate(template);

            var existingTemplate = await _templateRepository.GetByIdAsync(template.TemplateId);
            if (existingTemplate == null)
                throw new InvalidOperationException($"Template with ID {template.TemplateId} not found");

            // If template is published, don't allow direct updates
            if (existingTemplate.IsPublished)
                throw new InvalidOperationException("Published templates cannot be updated. Create a new version instead.");

            // Make sure we preserve certain values that should not be changed by client
            template.Version = existingTemplate.Version;
            template.CreatedAt = existingTemplate.CreatedAt;
            template.IsPublished = existingTemplate.IsPublished;
            
            _templateRepository.Update(template);
            await _templateRepository.SaveChangesAsync();

            return template;
        }

        public async Task<Template> PublishTemplateAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
                
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                throw new InvalidOperationException($"Template with ID {templateId} not found");

            // Validate template before publishing
            ValidateTemplateForPublishing(template);

            template.IsPublished = true;
            _templateRepository.Update(template);
            await _templateRepository.SaveChangesAsync();

            return template;
        }

        public async Task<Template> CreateNewVersionAsync(Guid templateId, Template updatedTemplate)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
                
            if (updatedTemplate == null)
                throw new ArgumentNullException(nameof(updatedTemplate));
                
            var currentTemplate = await _templateRepository.GetByIdAsync(templateId);
            if (currentTemplate == null)
                throw new InvalidOperationException($"Template with ID {templateId} not found");

            // Validate the updated template content
            ValidateTemplate(updatedTemplate);
            
            // Create a new version based on the updated template
            var newVersion = new Template
            {
                TemplateId = Guid.NewGuid(),
                Name = updatedTemplate.Name,
                Description = updatedTemplate.Description,
                Category = updatedTemplate.Category,
                Questions = updatedTemplate.Questions,
                ScoringRules = updatedTemplate.ScoringRules,
                ValidFrom = updatedTemplate.ValidFrom,
                ValidTo = updatedTemplate.ValidTo,
                CreatedById = updatedTemplate.CreatedById,
                IsPublished = false,
                Version = currentTemplate.Version + 1,
                CreatedAt = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(newVersion);
            await _templateRepository.SaveChangesAsync();

            return newVersion;
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
                
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return false;

            // Don't allow deletion of published templates that might be in use
            if (template.IsPublished)
            {
                // Check if template is in use before deleting
                // This would typically check related tables like assignments or audits
                // For now, we'll just throw an exception
                throw new InvalidOperationException("Cannot delete a published template. Unpublish it first.");
            }

            _templateRepository.Remove(template);
            return await _templateRepository.SaveChangesAsync();
        }
        
        public async Task<IEnumerable<Template>> GetTemplatesByRoleAsync(string role, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty", nameof(role));
                
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
                
            return await _templateRepository.GetTemplatesByRoleAsync(role, userId);
        }
        
        public async Task<IEnumerable<Template>> GetAssignedTemplatesAsync(Guid auditorId)
        {
            if (auditorId == Guid.Empty)
                throw new ArgumentException("Auditor ID cannot be empty", nameof(auditorId));
                
            return await _templateRepository.GetAssignedTemplatesAsync(auditorId);
        }
        
        private void ValidateTemplate(Template template)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
                throw new ArgumentException("Template name is required", nameof(template.Name));
                
            if (string.IsNullOrWhiteSpace(template.Category))
                throw new ArgumentException("Template category is required", nameof(template.Category));
                
            if (template.Questions == null)
                throw new ArgumentException("Template questions are required", nameof(template.Questions));
                
            if (template.ValidFrom.HasValue && template.ValidTo.HasValue && template.ValidFrom > template.ValidTo)
                throw new ArgumentException("Valid from date must be before valid to date");
                
            if (template.CreatedById == Guid.Empty)
                throw new ArgumentException("Creator ID must be specified", nameof(template.CreatedById));
        }
        
        private void ValidateTemplateForPublishing(Template template)
        {
            ValidateTemplate(template);
            
            // Additional validations specific to publishing
            if (string.IsNullOrWhiteSpace(template.Description))
                throw new InvalidOperationException("Template description is required for publishing");
                
            if (template.ScoringRules == null)
                throw new InvalidOperationException("Scoring rules are required for publishing");
        }
    }
}