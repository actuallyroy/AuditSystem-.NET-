using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;
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
                
            var template = await _templateRepository.GetByIdAsync(templateId);
            EnsureTemplateDateTimesAreUtc(template);
            return template;
        }

        public async Task<IEnumerable<Template>> GetAllTemplatesAsync()
        {
            return await _templateRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync()
        {
            return await _templateRepository.GetPublishedTemplatesAsync();
        }

        public async Task<IEnumerable<Template>> GetPublishedTemplatesAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
                
            return await _templateRepository.GetPublishedTemplatesAsync(userId);
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

        public async Task<IEnumerable<Template>> GetTemplatesByCategoryAsync(string category, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));
                
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
                
            return await _templateRepository.GetTemplatesByCategoryAsync(category, userId);
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            ValidateTemplate(template);

            // Check if template with same name already exists for this user
            if (await _templateRepository.TemplateNameExistsAsync(template.Name, template.CreatedById))
            {
                throw new InvalidOperationException($"A template with the name '{template.Name}' already exists. Please choose a different name.");
            }

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
            EnsureTemplateDateTimesAreUtc(existingTemplate);
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
            EnsureTemplateDateTimesAreUtc(template);
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
            EnsureTemplateDateTimesAreUtc(currentTemplate);
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
            EnsureTemplateDateTimesAreUtc(template);
            if (template == null)
                return false;

            // Allow deletion of published templates - related assignments and audits will be cascaded
            // Note: This will delete all related assignments and audits
            if (template.IsPublished)
            {
                // Log a warning that published template is being deleted
                // In a production system, you might want to add additional logging here
            }

            try
            {
                // Log the template details for debugging
                Console.WriteLine($"Attempting to delete template: {template.TemplateId}, Name: {template.Name}, IsPublished: {template.IsPublished}");
                
                _templateRepository.Remove(template);
                var result = await _templateRepository.SaveChangesAsync();
                
                // If SaveChangesAsync returns false, it might indicate an issue
                if (!result)
                {
                    throw new InvalidOperationException("SaveChangesAsync returned false - template deletion may have failed");
                }
                
                Console.WriteLine($"Template deletion successful: {template.TemplateId}");
                return result;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Handle database constraint violations
                var innerMessage = dbEx.InnerException?.Message ?? "No inner exception";
                var constraintMessage = $"Database constraint violation while deleting template: {dbEx.Message}. Inner: {innerMessage}";
                Console.WriteLine($"Database update exception: {constraintMessage}");
                throw new InvalidOperationException(constraintMessage, dbEx);
            }
            catch (Exception ex)
            {
                // Log the specific error for debugging
                var errorMessage = $"Failed to delete template: {ex.Message}. Type: {ex.GetType().Name}";
                throw new InvalidOperationException(errorMessage, ex);
            }
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
        
        public async Task<bool> TemplateNameExistsAsync(string name, Guid createdById)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name cannot be empty", nameof(name));
                
            if (createdById == Guid.Empty)
                throw new ArgumentException("Created By ID cannot be empty", nameof(createdById));
                
            return await _templateRepository.TemplateNameExistsAsync(name, createdById);
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

        private static void EnsureTemplateDateTimesAreUtc(Template template)
        {
            if (template == null) return;
            if (template.ValidFrom.HasValue && template.ValidFrom.Value.Kind != DateTimeKind.Utc)
                template.ValidFrom = DateTime.SpecifyKind(template.ValidFrom.Value, DateTimeKind.Utc);
            if (template.ValidTo.HasValue && template.ValidTo.Value.Kind != DateTimeKind.Utc)
                template.ValidTo = DateTime.SpecifyKind(template.ValidTo.Value, DateTimeKind.Utc);
            if (template.CreatedAt.Kind != DateTimeKind.Utc)
                template.CreatedAt = DateTime.SpecifyKind(template.CreatedAt, DateTimeKind.Utc);
        }
    }
}