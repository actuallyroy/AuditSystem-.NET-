using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class AssignmentsController : BaseApiController
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignmentsController> _logger;

        public AssignmentsController(
            IAssignmentService assignmentService,
            IUserService userService,
            INotificationService notificationService,
            ILogger<AssignmentsController> logger)
        {
            _assignmentService = assignmentService;
            _userService = userService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetAssignments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);
                
                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var assignments = await _assignmentService.GetAssignmentsByRoleAsync(
                    currentUserRole, currentUserId, currentUser.OrganisationId.Value);

                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AssignmentResponseDto>> GetAssignment(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Check if user can access this assignment
                var canAccess = await _assignmentService.CanUserAccessAssignmentAsync(currentUserId, id, currentUserRole);
                if (!canAccess)
                {
                    return Forbid();
                }

                var assignment = await _assignmentService.GetAssignmentByIdAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = $"Assignment with ID {id} not found" });
                }

                return Ok(ConvertToResponseDto(assignment));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignment with ID: {AssignmentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("organisation/{organisationId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetAssignmentsByOrganisation(Guid organisationId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Verify user can manage assignments for this organisation
                var canManage = await _assignmentService.CanUserManageAssignmentsAsync(currentUserId, organisationId, currentUserRole);
                if (!canManage)
                {
                    return Forbid();
                }

                var assignments = await _assignmentService.GetAssignmentsByOrganisationAsync(organisationId);
                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments for organisation: {OrganisationId}", organisationId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("auditor/{auditorId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetAssignmentsByAuditor(Guid auditorId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Get auditor details to verify same organisation
                var auditor = await _userService.GetUserByIdAsync(auditorId);
                if (auditor?.OrganisationId == null)
                {
                    return NotFound("Auditor not found or not assigned to an organisation");
                }

                var canManage = await _assignmentService.CanUserManageAssignmentsAsync(currentUserId, auditor.OrganisationId.Value, currentUserRole);
                if (!canManage)
                {
                    return Forbid();
                }

                var assignments = await _assignmentService.GetAssignmentsByAuditorAsync(auditorId);
                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments for auditor: {AuditorId}", auditorId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetAssignmentsByStatus(string status)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var assignments = await _assignmentService.GetAssignmentsByStatusAsync(status, currentUser.OrganisationId.Value);
                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments by status: {Status}", status);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("pending")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetPendingAssignments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var assignments = await _assignmentService.GetPendingAssignmentsAsync(currentUser.OrganisationId.Value);
                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending assignments");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("overdue")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDto>>> GetOverdueAssignments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var assignments = await _assignmentService.GetOverdueAssignmentsAsync(currentUser.OrganisationId.Value);
                var assignmentDtos = new List<AssignmentResponseDto>();
                foreach (var assignment in assignments)
                {
                    assignmentDtos.Add(ConvertToResponseDto(assignment));
                }

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue assignments");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AssignmentResponseDto>> CreateAssignment(CreateAssignmentDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                // Verify user can manage assignments for this organisation
                var canManage = await _assignmentService.CanUserManageAssignmentsAsync(currentUserId, currentUser.OrganisationId.Value, currentUserRole);
                if (!canManage)
                {
                    return Forbid();
                }

                var assignment = new Assignment
                {
                    TemplateId = request.TemplateId,
                    AssignedToId = request.AssignedToId,
                    AssignedById = currentUserId,
                    OrganisationId = currentUser.OrganisationId.Value,
                    DueDate = request.DueDate,
                    Priority = request.Priority ?? "medium",
                    Notes = request.Notes,
                    StoreInfo = request.StoreInfo
                };

                var createdAssignment = await _assignmentService.CreateAssignmentAsync(assignment);

                // Send notification to the assigned user
                try
                {
                    await _notificationService.SendAssignmentNotificationAsync(
                        createdAssignment.AssignmentId,
                        createdAssignment.AssignedToId,
                        createdAssignment.OrganisationId
                    );
                    _logger.LogInformation("Assignment notification sent to user {UserId} for assignment {AssignmentId}", 
                        createdAssignment.AssignedToId, createdAssignment.AssignmentId);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Failed to send assignment notification to user {UserId} for assignment {AssignmentId}", 
                        createdAssignment.AssignedToId, createdAssignment.AssignmentId);
                    // Don't fail the assignment creation if notification fails
                }

                return CreatedAtAction(
                    nameof(GetAssignment),
                    new { id = createdAssignment.AssignmentId },
                    ConvertToResponseDto(createdAssignment)
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("assign")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AssignmentResponseDto>> AssignTemplateToAuditor(AssignTemplateToAuditorDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var canManage = await _assignmentService.CanUserManageAssignmentsAsync(currentUserId, currentUser.OrganisationId.Value, currentUserRole);
                if (!canManage)
                {
                    return Forbid();
                }

                var assignmentDetails = new Assignment
                {
                    DueDate = request.DueDate,
                    Priority = request.Priority ?? "medium",
                    Notes = request.Notes,
                    StoreInfo = request.StoreInfo
                };

                var assignment = await _assignmentService.AssignTemplateToAuditorAsync(
                    request.TemplateId,
                    request.AuditorId,
                    currentUserId,
                    assignmentDetails
                );

                // Send notification to the assigned auditor
                try
                {
                    await _notificationService.SendAssignmentNotificationAsync(
                        assignment.AssignmentId,
                        assignment.AssignedToId,
                        assignment.OrganisationId
                    );
                    _logger.LogInformation("Assignment notification sent to auditor {AuditorId} for assignment {AssignmentId}", 
                        assignment.AssignedToId, assignment.AssignmentId);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Failed to send assignment notification to auditor {AuditorId} for assignment {AssignmentId}", 
                        assignment.AssignedToId, assignment.AssignmentId);
                    // Don't fail the assignment creation if notification fails
                }

                return CreatedAtAction(
                    nameof(GetAssignment),
                    new { id = assignment.AssignmentId },
                    ConvertToResponseDto(assignment)
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning template to auditor");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<AssignmentResponseDto>> UpdateAssignment(Guid id, UpdateAssignmentDto request)
        {
            if (id != request.AssignmentId)
                return BadRequest("Assignment ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Check if user can access this assignment
                var canAccess = await _assignmentService.CanUserAccessAssignmentAsync(currentUserId, id, currentUserRole);
                if (!canAccess)
                {
                    return Forbid();
                }

                // Get existing assignment to preserve certain fields
                var existingAssignment = await _assignmentService.GetAssignmentByIdAsync(id);
                if (existingAssignment == null)
                {
                    return NotFound(new { message = $"Assignment with ID {id} not found" });
                }

                var assignment = new Assignment
                {
                    AssignmentId = request.AssignmentId,
                    TemplateId = existingAssignment.TemplateId, // Don't allow changing template
                    AssignedToId = existingAssignment.AssignedToId, // Don't allow changing assignee
                    AssignedById = existingAssignment.AssignedById, // Don't allow changing assigner
                    OrganisationId = existingAssignment.OrganisationId, // Don't allow changing organisation
                    DueDate = request.DueDate,
                    Priority = request.Priority ?? existingAssignment.Priority,
                    Notes = request.Notes,
                    Status = request.Status ?? existingAssignment.Status,
                    StoreInfo = request.StoreInfo ?? existingAssignment.StoreInfo
                };

                var updatedAssignment = await _assignmentService.UpdateAssignmentAsync(assignment);

                return Ok(ConvertToResponseDto(updatedAssignment));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment with ID: {AssignmentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<AssignmentResponseDto>> UpdateAssignmentStatus(Guid id, UpdateAssignmentStatusDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var canAccess = await _assignmentService.CanUserAccessAssignmentAsync(currentUserId, id, currentUserRole);
                if (!canAccess)
                {
                    return Forbid();
                }

                var updatedAssignment = await _assignmentService.UpdateAssignmentStatusAsync(id, request.Status);

                return Ok(ConvertToResponseDto(updatedAssignment));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment status with ID: {AssignmentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> DeleteAssignment(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var canAccess = await _assignmentService.CanUserAccessAssignmentAsync(currentUserId, id, currentUserRole);
                if (!canAccess)
                {
                    return Forbid();
                }

                var result = await _assignmentService.DeleteAssignmentAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Assignment with ID {id} not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment with ID: {AssignmentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("unassign")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> UnassignTemplateFromAuditor(UnassignTemplateDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);

                if (currentUser?.OrganisationId == null)
                {
                    return BadRequest("User must belong to an organisation");
                }

                var canManage = await _assignmentService.CanUserManageAssignmentsAsync(currentUserId, currentUser.OrganisationId.Value, currentUserRole);
                if (!canManage)
                {
                    return Forbid();
                }

                var result = await _assignmentService.UnassignTemplateFromAuditorAsync(request.TemplateId, request.AuditorId);
                if (!result)
                {
                    return NotFound(new { message = "Assignment not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning template from auditor");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new InvalidOperationException("Invalid user identifier");
            }
            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Auditor";
        }

        private static AssignmentResponseDto ConvertToResponseDto(Assignment assignment)
        {
            return new AssignmentResponseDto
            {
                AssignmentId = assignment.AssignmentId,
                TemplateId = assignment.TemplateId,
                AssignedToId = assignment.AssignedToId,
                AssignedById = assignment.AssignedById,
                OrganisationId = assignment.OrganisationId,
                DueDate = assignment.DueDate,
                Priority = assignment.Priority,
                Notes = assignment.Notes,
                Status = assignment.Status,
                StoreInfo = assignment.StoreInfo?.RootElement.GetRawText(),
                CreatedAt = assignment.CreatedAt,
                // Navigation properties (if loaded)
                Template = assignment.Template != null ? new TemplateBasicDto
                {
                    TemplateId = assignment.Template.TemplateId,
                    Name = assignment.Template.Name,
                    Category = assignment.Template.Category
                } : null,
                AssignedTo = assignment.AssignedTo != null ? new UserBasicDto
                {
                    UserId = assignment.AssignedTo.UserId,
                    FirstName = assignment.AssignedTo.FirstName,
                    LastName = assignment.AssignedTo.LastName,
                    Email = assignment.AssignedTo.Email
                } : null,
                AssignedBy = assignment.AssignedBy != null ? new UserBasicDto
                {
                    UserId = assignment.AssignedBy.UserId,
                    FirstName = assignment.AssignedBy.FirstName,
                    LastName = assignment.AssignedBy.LastName,
                    Email = assignment.AssignedBy.Email
                } : null
            };
        }
    }

    #region DTOs

    public class AssignmentResponseDto
    {
        public Guid AssignmentId { get; set; }
        public Guid TemplateId { get; set; }
        public Guid AssignedToId { get; set; }
        public Guid AssignedById { get; set; }
        public Guid OrganisationId { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public string StoreInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public TemplateBasicDto Template { get; set; }
        public UserBasicDto AssignedTo { get; set; }
        public UserBasicDto AssignedBy { get; set; }
    }

    public class CreateAssignmentDto
    {
        public Guid TemplateId { get; set; }
        public Guid AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public JsonDocument StoreInfo { get; set; }
    }

    public class UpdateAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public JsonDocument StoreInfo { get; set; }
    }

    public class AssignTemplateToAuditorDto
    {
        public Guid TemplateId { get; set; }
        public Guid AuditorId { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public JsonDocument StoreInfo { get; set; }
    }

    public class UnassignTemplateDto
    {
        public Guid TemplateId { get; set; }
        public Guid AuditorId { get; set; }
    }

    public class UpdateAssignmentStatusDto
    {
        public string Status { get; set; }
    }

    public class TemplateBasicDto
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }

    public class UserBasicDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    #endregion
} 