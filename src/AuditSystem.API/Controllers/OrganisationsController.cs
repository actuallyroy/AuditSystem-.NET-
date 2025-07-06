using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class OrganisationsController : BaseApiController
    {
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly ILogger<OrganisationsController> _logger;

        public OrganisationsController(
            IOrganisationService organisationService,
            IUserService userService,
            ILogger<OrganisationsController> logger)
        {
            _organisationService = organisationService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<Organisation>>> GetOrganisations()
        {
            try
            {
                var organisations = await _organisationService.GetAllOrganisationsAsync();
                return Ok(organisations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organisations");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Organisation>> GetOrganisation(Guid id)
        {
            try
            {
                var organisation = await _organisationService.GetOrganisationByIdAsync(id);
                if (organisation == null)
                    return NotFound();

                return Ok(organisation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organisation with ID: {OrganisationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<Organisation>> CreateOrganisation(CreateOrganisationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var organisation = new Organisation
                {
                    Name = request.Name,
                    Region = request.Region,
                    Type = request.Type
                };

                var createdOrganisation = await _organisationService.CreateOrganisationAsync(organisation);
                return CreatedAtAction(nameof(GetOrganisation), new { id = createdOrganisation.OrganisationId }, createdOrganisation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organisation");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<Organisation>> UpdateOrganisation(Guid id, UpdateOrganisationRequest request)
        {
            if (id != request.OrganisationId)
                return BadRequest("Organisation ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var organisation = new Organisation
                {
                    OrganisationId = request.OrganisationId,
                    Name = request.Name,
                    Region = request.Region,
                    Type = request.Type
                };

                var updatedOrganisation = await _organisationService.UpdateOrganisationAsync(organisation);
                return Ok(updatedOrganisation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organisation with ID: {OrganisationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> DeleteOrganisation(Guid id)
        {
            try
            {
                var result = await _organisationService.DeleteOrganisationAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organisation with ID: {OrganisationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // Endpoints for organization invitations

        [HttpPost("{id:guid}/invite")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> InviteUser(Guid id, InviteUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _organisationService.InviteUserToOrganisationAsync(id, request.Email, request.Role);
                return Ok(new { message = "Invitation sent successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user to organisation: {OrganisationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("join/{id:guid}")]
        [Authorize]
        public async Task<ActionResult> JoinOrganisation(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return BadRequest("Invalid user identifier");
                }

                var result = await _organisationService.JoinOrganisationAsync(userId, id);
                if (!result)
                    return BadRequest("Failed to join organisation");

                return Ok(new { message = "Successfully joined organisation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining organisation with ID: {OrganisationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        public async Task<ActionResult> AcceptInvitation(AcceptInvitationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _organisationService.AcceptInvitationAsync(request.Token);
                if (!result)
                    return BadRequest("Invalid or expired invitation token");

                return Ok(new { message = "Invitation accepted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation with token: {Token}", request.Token);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{organisationId:guid}/users/{userId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult> RemoveUserFromOrganisation(Guid organisationId, Guid userId)
        {
            try
            {
                var result = await _organisationService.RemoveUserFromOrganisationAsync(userId, organisationId);
                if (!result)
                    return BadRequest("Failed to remove user from organisation");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from organisation {OrganisationId}", 
                    userId, organisationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("available")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Organisation>>> GetAvailableOrganisations()
        {
            try
            {
                var organisations = await _organisationService.GetOrganisationsForJoiningAsync();
                return Ok(organisations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available organisations for joining");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    #region Request Models
    public class CreateOrganisationRequest
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Type { get; set; }
    }

    public class UpdateOrganisationRequest
    {
        public Guid OrganisationId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string Type { get; set; }
    }

    public class InviteUserRequest
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class AcceptInvitationRequest
    {
        public string Token { get; set; }
    }
    #endregion
} 