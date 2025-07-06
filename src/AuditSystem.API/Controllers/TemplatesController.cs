using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using AuditSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class TemplatesController : BaseApiController
    {
        private readonly ITemplateService _templateService;
        
        public TemplatesController(ITemplateService templateService)
        {
            _templateService = templateService;
        }
        
        [HttpGet]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<TemplateResponseDto>>> GetTemplates()
        {
            try
            {
                // Get current user's ID
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Only return templates created by the requesting user
                var templates = await _templateService.GetTemplatesByUserAsync(currentUserId);
                var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving templates: {ex.Message}" });
            }
        }
        
        [HttpGet("{id}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<TemplateResponseDto>> GetTemplate(Guid id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                
                if (template == null)
                    return NotFound(new { message = $"Template with ID {id} not found" });
                
                // Check if the user has access to this template
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Auditor";
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Admin can access any template
                if (currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(ConvertToResponseDto(template));
                }
                
                // Manager can access their own templates and published ones
                if (currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    if (template.CreatedById == currentUserId || template.IsPublished)
                    {
                        return Ok(ConvertToResponseDto(template));
                    }
                }
                
                // Supervisor can access only published templates
                if (currentUserRole.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
                {
                    if (template.IsPublished)
                    {
                        return Ok(ConvertToResponseDto(template));
                    }
                }
                
                // Auditor can only access templates assigned to them
                if (currentUserRole.Equals("Auditor", StringComparison.OrdinalIgnoreCase))
                {
                    var assignedTemplates = await _templateService.GetAssignedTemplatesAsync(currentUserId);
                    if (assignedTemplates.Any(t => t.TemplateId == id))
                    {
                        return Ok(ConvertToResponseDto(template));
                    }
                }
                
                // If not accessible, return forbidden
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving template: {ex.Message}" });
            }
        }
        
        [HttpGet("published")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<TemplateResponseDto>>> GetPublishedTemplates()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                
                // Admin can see all published templates
                if (currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    var templates = await _templateService.GetPublishedTemplatesAsync();
                    var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                    return Ok(templateDtos);
                }
                else
                {
                    // Other roles see only published templates from their organization
                    var templates = await _templateService.GetPublishedTemplatesAsync(currentUserId);
                    var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                    return Ok(templateDtos);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving published templates: {ex.Message}" });
            }
        }
        
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<TemplateResponseDto>>> GetTemplatesByUser(Guid userId)
        {
            try
            {
                // Security check - only allow users to see their own templates unless admin or manager
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                
                if (!isAdmin && !isManager && currentUserId != userId)
                    return Forbid();
                    
                var templates = await _templateService.GetTemplatesByUserAsync(userId);
                var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving templates: {ex.Message}" });
            }
        }
        
        [HttpGet("category/{category}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<TemplateResponseDto>>> GetTemplatesByCategory(string category)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                
                // Admin can see all templates by category
                if (currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    var templates = await _templateService.GetTemplatesByCategoryAsync(category);
                    var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                    return Ok(templateDtos);
                }
                else
                {
                    // Other roles see only templates by category from their organization
                    var templates = await _templateService.GetTemplatesByCategoryAsync(category, currentUserId);
                    var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                    return Ok(templateDtos);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving templates by category: {ex.Message}" });
            }
        }
        
        [HttpGet("assigned")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<TemplateResponseDto>>> GetAssignedTemplates()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var templates = await _templateService.GetAssignedTemplatesAsync(currentUserId);
                var templateDtos = templates.Select(t => ConvertToResponseDto(t)).ToList();
                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred retrieving assigned templates: {ex.Message}" });
            }
        }
        
        [HttpPost]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<TemplateResponseDto>> CreateTemplate(CreateTemplateDto templateDto)
        {
            if (templateDto == null)
                return BadRequest(new { message = "Template data is required" });
                
            try
            {
                // Convert DTO to entity
                var template = new Template
                {
                    Name = templateDto.Name,
                    Description = templateDto.Description,
                    Category = templateDto.Category,
                    Questions = templateDto.Questions.HasValue ? JsonDocument.Parse(templateDto.Questions.Value.GetRawText()) : null,
                    ScoringRules = templateDto.ScoringRules.HasValue ? JsonDocument.Parse(templateDto.ScoringRules.Value.GetRawText()) : null,
                    ValidFrom = templateDto.ValidFrom.HasValue ? DateTime.SpecifyKind(templateDto.ValidFrom.Value, DateTimeKind.Utc) : null,
                    ValidTo = templateDto.ValidTo.HasValue ? DateTime.SpecifyKind(templateDto.ValidTo.Value, DateTimeKind.Utc) : null,
                    CreatedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                };
                
                var createdTemplate = await _templateService.CreateTemplateAsync(template);
                
                return CreatedAtAction(
                    nameof(GetTemplate), 
                    new { id = createdTemplate.TemplateId }, 
                    ConvertToResponseDto(createdTemplate)
                );
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred creating the template: {ex.Message}" });
            }
        }
        
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<TemplateResponseDto>> UpdateTemplate(Guid id, UpdateTemplateDto updateDto)
        {
            if (updateDto == null)
                return BadRequest(new { message = "Template data is required" });
                
            try
            {
                // Get existing template
                var existingTemplate = await _templateService.GetTemplateByIdAsync(id);
                if (existingTemplate == null)
                    return NotFound(new { message = $"Template with ID {id} not found" });
                
                // Security check - only allow template creator or admin to update
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                
                if (!isAdmin && currentUserId != existingTemplate.CreatedById)
                    return Forbid();
                
                // Update template properties
                existingTemplate.Name = updateDto.Name ?? existingTemplate.Name;
                existingTemplate.Description = updateDto.Description ?? existingTemplate.Description;
                existingTemplate.Category = updateDto.Category ?? existingTemplate.Category;
                
                if (updateDto.Questions.HasValue)
                    existingTemplate.Questions = JsonDocument.Parse(updateDto.Questions.Value.GetRawText());
                
                if (updateDto.ScoringRules.HasValue)
                    existingTemplate.ScoringRules = JsonDocument.Parse(updateDto.ScoringRules.Value.GetRawText());
                
                if (updateDto.ValidFrom.HasValue)
                    existingTemplate.ValidFrom = DateTime.SpecifyKind(updateDto.ValidFrom.Value, DateTimeKind.Utc);
                
                if (updateDto.ValidTo.HasValue)
                    existingTemplate.ValidTo = DateTime.SpecifyKind(updateDto.ValidTo.Value, DateTimeKind.Utc);
                
                var updatedTemplate = await _templateService.UpdateTemplateAsync(existingTemplate);
                return Ok(ConvertToResponseDto(updatedTemplate));
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred updating the template: {ex.Message}" });
            }
        }
        
        [HttpPut("{id}/publish")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<TemplateResponseDto>> PublishTemplate(Guid id)
        {
            try
            {
                // Security check - only allow template creator or admin to publish
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                    return NotFound(new { message = $"Template with ID {id} not found" });
                    
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                
                if (!isAdmin && currentUserId != template.CreatedById)
                    return Forbid();
                    
                var publishedTemplate = await _templateService.PublishTemplateAsync(id);
                return Ok(ConvertToResponseDto(publishedTemplate));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred publishing the template: {ex.Message}" });
            }
        }
        
        [HttpPost("{id}/version")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<TemplateResponseDto>> CreateNewVersion(Guid id, UpdateTemplateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return BadRequest(new { message = "Template data is required" });
                    
                // Security check
                var template = await _templateService.GetTemplateByIdAsync(id);
                
                if (template == null)
                    return NotFound(new { message = $"Template with ID {id} not found" });
                    
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                
                if (!isAdmin && currentUserId != template.CreatedById)
                    return Forbid();
                
                // Convert DTO to entity for new version
                var updatedTemplate = new Template
                {
                    Name = updateDto.Name ?? template.Name,
                    Description = updateDto.Description ?? template.Description,
                    Category = updateDto.Category ?? template.Category,
                    Questions = updateDto.Questions.HasValue 
                        ? JsonDocument.Parse(updateDto.Questions.Value.GetRawText()) 
                        : template.Questions,
                    ScoringRules = updateDto.ScoringRules.HasValue 
                        ? JsonDocument.Parse(updateDto.ScoringRules.Value.GetRawText()) 
                        : template.ScoringRules,
                    ValidFrom = updateDto.ValidFrom.HasValue 
                        ? DateTime.SpecifyKind(updateDto.ValidFrom.Value, DateTimeKind.Utc) 
                        : template.ValidFrom,
                    ValidTo = updateDto.ValidTo.HasValue 
                        ? DateTime.SpecifyKind(updateDto.ValidTo.Value, DateTimeKind.Utc) 
                        : template.ValidTo,
                    CreatedById = currentUserId
                };
                
                var newVersion = await _templateService.CreateNewVersionAsync(id, updatedTemplate);
                
                return CreatedAtAction(
                    nameof(GetTemplate), 
                    new { id = newVersion.TemplateId }, 
                    ConvertToResponseDto(newVersion)
                );
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred creating a new version: {ex.Message}" });
            }
        }
        
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            try
            {
                // Security check - only allow template creator or admin to delete
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                    return NotFound(new { message = $"Template with ID {id} not found" });
                    
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                
                if (!isAdmin && currentUserId != template.CreatedById)
                    return Forbid();
                    
                var result = await _templateService.DeleteTemplateAsync(id);
                
                if (result)
                    return NoContent();
                else
                    return StatusCode(500, new { message = "Failed to delete template" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred deleting the template: {ex.Message}" });
            }
        }
        
        private TemplateResponseDto ConvertToResponseDto(Template template)
        {
            return new TemplateResponseDto
            {
                TemplateId = template.TemplateId,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Questions = template.Questions?.RootElement,
                ScoringRules = template.ScoringRules?.RootElement,
                ValidFrom = template.ValidFrom,
                ValidTo = template.ValidTo,
                CreatedById = template.CreatedById,
                IsPublished = template.IsPublished,
                Version = template.Version,
                CreatedAt = template.CreatedAt
            };
        }
    }
}