using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using AuditSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var userDtos = users.Select(MapToUserDto);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(MapToUserDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-username/{username}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                    return NotFound();

                return Ok(MapToUserDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-organisation/{organisationId:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByOrganisation(Guid organisationId)
        {
            try
            {
                var users = await _userService.GetUsersByOrganisationAsync(organisationId);
                var userDtos = users.Select(MapToUserDto);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by organisation ID: {OrganisationId}", organisationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-role/{role}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(string role)
        {
            try
            {
                var users = await _userService.GetUsersByRoleAsync(role);
                var userDtos = users.Select(MapToUserDto);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role: {Role}", role);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = new User
                {
                    OrganisationId = request.OrganisationId,
                    Username = request.Username,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Role = request.Role
                };

                var createdUser = await _userService.CreateUserAsync(user, request.Password);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, MapToUserDto(createdUser));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, UpdateUserDto request)
        {
            if (id != request.UserId)
                return BadRequest("User ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = new User
                {
                    UserId = request.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Role = request.Role,
                    IsActive = request.IsActive
                };

                var updatedUser = await _userService.UpdateUserAsync(user);
                return Ok(MapToUserDto(updatedUser));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id:guid}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> DeactivateUser(Guid id)
        {
            try
            {
                var result = await _userService.DeactivateUserAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user with ID: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id:guid}/change-password")]
        public async Task<ActionResult> ChangePassword(Guid id, ChangePasswordDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.UpdateUserPasswordAsync(id, request.CurrentPassword, request.NewPassword);
                if (!result)
                    return BadRequest("Invalid current password");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user with ID: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                OrganisationId = user.OrganisationId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }


} 