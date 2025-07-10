using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using AuditSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class AuditsController : BaseApiController
    {
        private readonly IAuditService _auditService;
        private readonly IAssignmentService _assignmentService;
        private readonly ILogger<AuditsController> _logger;

        public AuditsController(IAuditService auditService, IAssignmentService assignmentService, ILogger<AuditsController> logger)
        {
            _auditService = auditService;
            _assignmentService = assignmentService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<AuditSummaryDto>>> GetAudits()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

                IEnumerable<Audit> audits;

                // Role-based access control
                if (currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                    currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    // Admins and Managers can see all audits
                    audits = await _auditService.GetAllAuditsAsync();
                }
                else
                {
                    // Auditors and Supervisors can only see their own audits
                    audits = await _auditService.GetAuditsByAuditorAsync(currentUserId);
                }

                var auditSummaries = new List<AuditSummaryDto>();
                foreach (var audit in audits)
                {
                    auditSummaries.Add(await MapToAuditSummaryDtoAsync(audit));
                }
                
                return Ok(auditSummaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audits");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AuditResponseDto>> GetAudit(Guid id)
        {
            try
            {
                var audit = await _auditService.GetAuditByIdAsync(id);
                if (audit == null)
                    return NotFound(new { message = "Audit not found" });

                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

                // Check access permissions
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                var isOwner = audit.AuditorId == currentUserId;

                if (!isAdmin && !isManager && !isOwner)
                    return Forbid();

                return Ok(MapToAuditResponseDto(audit));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("by-auditor/{auditorId:guid}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<AuditSummaryDto>>> GetAuditsByAuditor(Guid auditorId)
        {
            try
            {
                var audits = await _auditService.GetAuditsByAuditorAsync(auditorId);
                var auditSummaries = new List<AuditSummaryDto>();
                foreach (var audit in audits)
                {
                    auditSummaries.Add(await MapToAuditSummaryDtoAsync(audit));
                }
                return Ok(auditSummaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audits for auditor: {AuditorId}", auditorId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("by-organisation/{organisationId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AuditSummaryDto>>> GetAuditsByOrganisation(Guid organisationId)
        {
            try
            {
                var audits = await _auditService.GetAuditsByOrganisationAsync(organisationId);
                var auditSummaries = new List<AuditSummaryDto>();
                foreach (var audit in audits)
                {
                    auditSummaries.Add(await MapToAuditSummaryDtoAsync(audit));
                }
                return Ok(auditSummaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audits for organisation: {OrganisationId}", organisationId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("by-template/{templateId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AuditSummaryDto>>> GetAuditsByTemplate(Guid templateId)
        {
            try
            {
                var audits = await _auditService.GetAuditsByTemplateAsync(templateId);
                var auditSummaries = new List<AuditSummaryDto>();
                foreach (var audit in audits)
                {
                    auditSummaries.Add(await MapToAuditSummaryDtoAsync(audit));
                }
                return Ok(auditSummaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audits for template: {TemplateId}", templateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<AuditResponseDto>> CreateAudit(CreateAuditDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                
                // Check user roles
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                
                // Get organization ID from JWT claims instead of hardcoded value
                var orgIdClaim = User.FindFirstValue("organisation_id");
                if (string.IsNullOrEmpty(orgIdClaim))
                    return BadRequest(new { message = "User organization not found in token" });
                
                var organisationId = Guid.Parse(orgIdClaim);
                
                // Get the assignment to validate it exists and user has access
                var assignment = await _assignmentService.GetAssignmentByIdAsync(createDto.AssignmentId);
                if (assignment == null)
                    return NotFound(new { message = "Assignment not found" });

                // Check if the current user is assigned to this assignment
                if (assignment.AssignedToId != currentUserId && !isAdmin && !isManager)
                    return Forbid();

                // Check if an audit already exists for this assignment
                var existingAudit = await _auditService.GetAuditByAssignmentAsync(createDto.AssignmentId);
                if (existingAudit != null)
                {
                    // Update the existing audit instead of creating a new one
                    return await UpdateExistingAudit(existingAudit, createDto, currentUserId, isAdmin, isManager);
                }

                // Convert assignment to audit
                JsonDocument storeInfo = null;
                JsonDocument location = null;

                if (createDto.StoreInfo.HasValue)
                    storeInfo = JsonDocument.Parse(createDto.StoreInfo.Value.GetRawText());

                if (createDto.Location.HasValue)
                    location = JsonDocument.Parse(createDto.Location.Value.GetRawText());
                
                var audit = await _auditService.StartAuditFromAssignmentAsync(
                    createDto.TemplateId, 
                    currentUserId, 
                    organisationId, 
                    createDto.AssignmentId, 
                    storeInfo, 
                    location);

                // Set store information
                if (!string.IsNullOrEmpty(createDto.StoreName) || !string.IsNullOrEmpty(createDto.StoreLocation))
                {
                    var storeInfoJson = new Dictionary<string, object>();
                    
                    if (!string.IsNullOrEmpty(createDto.StoreName))
                        storeInfoJson["storeName"] = createDto.StoreName;
                        
                    if (!string.IsNullOrEmpty(createDto.StoreLocation))
                        storeInfoJson["storeLocation"] = createDto.StoreLocation;
                    
                    // Merge with existing store info if provided
                    if (createDto.StoreInfo.HasValue)
                    {
                        var existingInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            createDto.StoreInfo.Value.GetRawText());
                        foreach (var kvp in existingInfo)
                            storeInfoJson[kvp.Key] = kvp.Value;
                    }
                    
                    audit.StoreInfo = JsonDocument.Parse(JsonSerializer.Serialize(storeInfoJson));
                }
                else if (createDto.StoreInfo.HasValue)
                {
                    audit.StoreInfo = JsonDocument.Parse(createDto.StoreInfo.Value.GetRawText());
                }

                // Set location information
                if (createDto.Location.HasValue)
                {
                    audit.Location = JsonDocument.Parse(createDto.Location.Value.GetRawText());
                }

                // Set responses if provided (for complete audit creation)
                if (createDto.Responses.HasValue)
                {
                    audit.Responses = JsonDocument.Parse(createDto.Responses.Value.GetRawText());
                    
                    // Set critical issues count
                    audit.CriticalIssues = createDto.CriticalIssues ?? 0;
                    
                    // Calculate score if not provided
                    if (createDto.Score.HasValue)
                    {
                        audit.Score = createDto.Score.Value;
                    }
                    else
                    {
                        // Calculate score based on responses and template scoring rules
                        audit.Score = await _auditService.CalculateAuditScoreAsync(audit);
                    }
                }

                // Set media if provided
                if (createDto.Media.HasValue)
                {
                    audit.Media = JsonDocument.Parse(createDto.Media.Value.GetRawText());
                }

                // Set status
                if (!string.IsNullOrEmpty(createDto.Status))
                {
                    audit.Status = createDto.Status;
                }

                // If audit has responses, submit it to save all the data
                if (createDto.Responses.HasValue)
                {
                    var responsesElement = createDto.Responses.Value;
                    bool isEmpty = false;
                    if (responsesElement.ValueKind == JsonValueKind.Object && responsesElement.EnumerateObject().MoveNext() == false)
                    {
                        isEmpty = true;
                    }
                    if (!isEmpty)
                    {
                        audit = await _auditService.SubmitAuditAsync(audit);
                    }
                }

                return CreatedAtAction(nameof(GetAudit), new { id = audit.AuditId }, MapToAuditResponseDto(audit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id:guid}/submit")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<AuditResponseDto>> SubmitAudit(Guid id, SubmitAuditDto submitDto)
        {
            if (id != submitDto.AuditId)
                return BadRequest(new { message = "Audit ID mismatch" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingAudit = await _auditService.GetAuditByIdAsync(id);
                if (existingAudit == null)
                    return NotFound(new { message = "Audit not found" });

                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

                // Check if user can submit this audit
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                var isOwner = existingAudit.AuditorId == currentUserId;

                if (!isAdmin && !isManager && !isOwner)
                    return Forbid();

                // Create audit object with submitted data
                var auditToSubmit = new Audit
                {
                    AuditId = submitDto.AuditId,
                    Responses = JsonDocument.Parse(submitDto.Responses.GetRawText())
                };

                if (submitDto.Media.HasValue)
                    auditToSubmit.Media = JsonDocument.Parse(submitDto.Media.Value.GetRawText());

                if (submitDto.StoreInfo.HasValue)
                    auditToSubmit.StoreInfo = JsonDocument.Parse(submitDto.StoreInfo.Value.GetRawText());

                if (submitDto.Location.HasValue)
                    auditToSubmit.Location = JsonDocument.Parse(submitDto.Location.Value.GetRawText());

                var submittedAudit = await _auditService.SubmitAuditAsync(auditToSubmit);
                return Ok(MapToAuditResponseDto(submittedAudit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AuditResponseDto>> UpdateAuditStatus(Guid id, UpdateAuditStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var audit = await _auditService.UpdateAuditStatusAsync(id, statusDto.Status);
                if (audit == null)
                    return NotFound(new { message = $"Audit with ID {id} not found." });

                // Add manager notes if provided
                if (!string.IsNullOrEmpty(statusDto.ManagerNotes))
                {
                    audit = await _auditService.AddManagerNotesAsync(id, statusDto.ManagerNotes);
                }

                // Update flag status if provided
                if (statusDto.IsFlagged.HasValue)
                {
                    audit = await _auditService.FlagAuditAsync(id, statusDto.IsFlagged.Value);
                }

                return Ok(MapToAuditResponseDto(audit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating audit status for ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("{id:guid}/flag")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AuditResponseDto>> FlagAudit(Guid id, [FromBody] bool isFlagged)
        {
            try
            {
                var audit = await _auditService.FlagAuditAsync(id, isFlagged);
                return Ok(MapToAuditResponseDto(audit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("{id:guid}/recalculate-score")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AuditResponseDto>> RecalculateAuditScore(Guid id)
        {
            try
            {
                var audit = await _auditService.RecalculateAuditScoreAsync(id);
                return Ok(MapToAuditResponseDto(audit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating score for audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<AuditResponseDto>> UpdateAudit(Guid id, UpdateAuditDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingAudit = await _auditService.GetAuditByIdAsync(id);
                if (existingAudit == null)
                    return NotFound(new { message = "Audit not found" });

                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

                // Check if user can update this audit
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                var isOwner = existingAudit.AuditorId == currentUserId;

                if (!isAdmin && !isManager && !isOwner)
                    return Forbid();

                // Update audit fields
                if (updateDto.Responses.HasValue)
                {
                    existingAudit.Responses = JsonDocument.Parse(updateDto.Responses.Value.GetRawText());
                }

                if (updateDto.Media.HasValue)
                {
                    existingAudit.Media = JsonDocument.Parse(updateDto.Media.Value.GetRawText());
                }

                if (updateDto.StoreInfo.HasValue)
                {
                    existingAudit.StoreInfo = JsonDocument.Parse(updateDto.StoreInfo.Value.GetRawText());
                }

                if (updateDto.Location.HasValue)
                {
                    existingAudit.Location = JsonDocument.Parse(updateDto.Location.Value.GetRawText());
                }

                if (updateDto.CriticalIssues.HasValue)
                {
                    existingAudit.CriticalIssues = updateDto.CriticalIssues.Value;
                }

                if (!string.IsNullOrEmpty(updateDto.Status))
                {
                    existingAudit.Status = updateDto.Status;
                }

                // Recalculate score if responses were updated
                if (updateDto.Responses.HasValue)
                {
                    existingAudit.Score = await _auditService.CalculateAuditScoreAsync(existingAudit);
                }

                // Update the audit (not submit)
                var updatedAudit = await _auditService.UpdateAuditAsync(existingAudit);
                return Ok(MapToAuditResponseDto(updatedAudit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> DeleteAudit(Guid id)
        {
            try
            {
                var existingAudit = await _auditService.GetAuditByIdAsync(id);
                if (existingAudit == null)
                    return NotFound(new { message = "Audit not found" });

                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

                // Check if user can delete this audit
                var isAdmin = currentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                var isManager = currentUserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);
                var isOwner = existingAudit.AuditorId == currentUserId;

                if (!isAdmin && !isManager && !isOwner)
                    return Forbid();

                // Delete the audit
                var success = await _auditService.DeleteAuditAsync(id);
                if (success)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to delete audit" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit with ID: {AuditId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private async Task<ActionResult<AuditResponseDto>> UpdateExistingAudit(Audit existingAudit, CreateAuditDto createDto, Guid currentUserId, bool isAdmin, bool isManager)
        {
            try
            {
                // Check if user can update this audit
                if (existingAudit.AuditorId != currentUserId && !isAdmin && !isManager)
                    return Forbid();

                // Update store information
                if (!string.IsNullOrEmpty(createDto.StoreName) || !string.IsNullOrEmpty(createDto.StoreLocation))
                {
                    var storeInfoJson = new Dictionary<string, object>();
                    
                    if (!string.IsNullOrEmpty(createDto.StoreName))
                        storeInfoJson["storeName"] = createDto.StoreName;
                        
                    if (!string.IsNullOrEmpty(createDto.StoreLocation))
                        storeInfoJson["storeLocation"] = createDto.StoreLocation;
                    
                    // Merge with existing store info if provided
                    if (createDto.StoreInfo.HasValue)
                    {
                        var existingInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            createDto.StoreInfo.Value.GetRawText());
                        foreach (var kvp in existingInfo)
                            storeInfoJson[kvp.Key] = kvp.Value;
                    }
                    
                    existingAudit.StoreInfo = JsonDocument.Parse(JsonSerializer.Serialize(storeInfoJson));
                }
                else if (createDto.StoreInfo.HasValue)
                {
                    existingAudit.StoreInfo = JsonDocument.Parse(createDto.StoreInfo.Value.GetRawText());
                }

                // Update location information
                if (createDto.Location.HasValue)
                {
                    existingAudit.Location = JsonDocument.Parse(createDto.Location.Value.GetRawText());
                }

                // Update responses if provided
                if (createDto.Responses.HasValue)
                {
                    existingAudit.Responses = JsonDocument.Parse(createDto.Responses.Value.GetRawText());
                    
                    // Set critical issues count
                    existingAudit.CriticalIssues = createDto.CriticalIssues ?? 0;
                    
                    // Calculate score if not provided
                    if (createDto.Score.HasValue)
                    {
                        existingAudit.Score = createDto.Score.Value;
                    }
                    else
                    {
                        // Calculate score based on responses and template scoring rules
                        existingAudit.Score = await _auditService.CalculateAuditScoreAsync(existingAudit);
                    }
                }

                // Update media if provided
                if (createDto.Media.HasValue)
                {
                    existingAudit.Media = JsonDocument.Parse(createDto.Media.Value.GetRawText());
                }

                // Update status if provided
                if (!string.IsNullOrEmpty(createDto.Status))
                {
                    existingAudit.Status = createDto.Status;
                }

                // Save the audit to persist all changes (status, store info, location, etc.)
                // If audit has responses, submit it to save all the data
                if (createDto.Responses.HasValue)
                {
                    var responsesElement = createDto.Responses.Value;
                    bool isEmpty = false;
                    if (responsesElement.ValueKind == JsonValueKind.Object && responsesElement.EnumerateObject().MoveNext() == false)
                    {
                        isEmpty = true;
                    }
                    if (!isEmpty)
                    {
                        existingAudit = await _auditService.SubmitAuditAsync(existingAudit);
                    }
                    else
                    {
                        // Even if responses are empty, we need to save other changes
                        existingAudit = await _auditService.UpdateAuditAsync(existingAudit);
                    }
                }
                else
                {
                    // No responses provided, but we need to save other changes (status, store info, etc.)
                    existingAudit = await _auditService.UpdateAuditAsync(existingAudit);
                }

                return Ok(MapToAuditResponseDto(existingAudit));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { message = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating existing audit with ID: {AuditId}", existingAudit.AuditId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private static AuditResponseDto MapToAuditResponseDto(Audit audit)
        {
            return new AuditResponseDto
            {
                AuditId = audit.AuditId,
                TemplateId = audit.TemplateId,
                TemplateVersion = audit.TemplateVersion,
                AuditorId = audit.AuditorId,
                OrganisationId = audit.OrganisationId,
                AssignmentId = audit.AssignmentId,
                Status = audit.Status,
                StartTime = audit.StartTime,
                EndTime = audit.EndTime,
                StoreInfo = audit.StoreInfo?.RootElement,
                Responses = audit.Responses?.RootElement,
                Media = audit.Media?.RootElement,
                Location = audit.Location?.RootElement,
                Score = audit.Score,
                CriticalIssues = audit.CriticalIssues,
                ManagerNotes = audit.ManagerNotes,
                IsFlagged = audit.IsFlagged,
                SyncFlag = audit.SyncFlag,
                CreatedAt = audit.CreatedAt,
                TemplateName = audit.Template?.Name ?? "",
                AuditorName = audit.Auditor != null ? $"{audit.Auditor.FirstName} {audit.Auditor.LastName}" : "",
                OrganisationName = audit.Organisation?.Name ?? ""
            };
        }

        private async Task<AuditSummaryDto> MapToAuditSummaryDtoAsync(Audit audit)
        {
            // Extract store information from JSON
            string? storeId = null;
            string? storeName = null;
            string? address = null;
            
            if (audit.StoreInfo?.RootElement.ValueKind == JsonValueKind.Object)
            {
                var storeInfo = audit.StoreInfo.RootElement;
                if (storeInfo.TryGetProperty("storeId", out var storeIdElement))
                    storeId = storeIdElement.GetString();
                if (storeInfo.TryGetProperty("storeName", out var storeNameElement))
                    storeName = storeNameElement.GetString();
                if (storeInfo.TryGetProperty("storeAddress", out var storeAddressElement))
                    address = storeAddressElement.GetString();
                else if (storeInfo.TryGetProperty("address", out var addressElement))
                    address = addressElement.GetString();
                else if (storeInfo.TryGetProperty("storeLocation", out var locationElement))
                    address = locationElement.GetString();
            }
            
            // Determine rejection reason based on status and manager notes
            string? rejectionReason = null;
            if (audit.Status?.Equals("rejected", StringComparison.OrdinalIgnoreCase) == true)
            {
                rejectionReason = audit.ManagerNotes;
            }
            
            // Get assignment ID from audit entity
            Guid? assignmentId = audit.AssignmentId;
            
            return new AuditSummaryDto
            {
                AuditId = audit.AuditId,
                TemplateId = audit.TemplateId,
                TemplateName = audit.Template?.Name ?? "",
                StoreId = storeId,
                StoreName = storeName,
                Address = address,
                AuditorId = audit.AuditorId,
                AuditorName = audit.Auditor != null ? $"{audit.Auditor.FirstName} {audit.Auditor.LastName}" : "",
                OrganisationId = audit.OrganisationId,
                OrganisationName = audit.Organisation?.Name ?? "",
                Status = audit.Status,
                Score = audit.Score,
                CriticalIssues = audit.CriticalIssues,
                IsFlagged = audit.IsFlagged,
                CreatedAt = audit.CreatedAt,
                EndTime = audit.EndTime,
                RejectionReason = rejectionReason,
                Notes = audit.ManagerNotes,
                AssignmentId = assignmentId
            };
        }
    }
} 