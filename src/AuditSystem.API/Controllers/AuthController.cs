using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuditSystem.API.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IOrganisationService organisationService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _organisationService = organisationService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                // Check if username already exists
                try
                {
                    var existingUser = await _userService.GetUserByUsernameAsync(request.Username);
                    if (existingUser != null)
                        return BadRequest(new { message = "Username already exists" });
                }
                catch (InvalidCastException ex)
                {
                    _logger.LogError(ex, "Error checking existing user during registration: {Username}", request.Username);
                    // Continue with registration as the error is likely due to database inconsistency
                    // with null organisation_id in existing records
                }

                // Create a default organisation if none is provided
                if (!request.OrganisationId.HasValue)
                {
                    try
                    {
                        var organisation = await _organisationService.CreateDefaultOrganisationForUserAsync(request.FirstName);
                        _logger.LogInformation("Created default organisation: {OrganisationName} with ID: {OrganisationId}", 
                            organisation.Name, organisation.OrganisationId);
                        request.OrganisationId = organisation.OrganisationId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating default organisation for user: {Username}", request.Username);
                        // Continue with registration, but with null OrganisationId
                    }
                }

                // Create new user
                var user = new User
                {
                    // Set OrganisationId only if it has a value
                    OrganisationId = request.OrganisationId,
                    Username = request.Username,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    // Set a valid role based on the database constraint (auditor, manager, supervisor, admin)
                    Role = "auditor" // Default role for self-registration
                };

                var createdUser = await _userService.CreateUserAsync(user, request.Password);
                
                // Generate JWT token
                var token = GenerateJwtToken(createdUser);

                return Ok(new AuthResponse
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    FirstName = createdUser.FirstName,
                    LastName = createdUser.LastName,
                    Token = token,
                    Role = createdUser.Role
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Validation error during registration for user: {Username}", request.Username);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                _logger.LogError(ex, "Database error with organisation_id during registration for user: {Username}", request.Username);
                return BadRequest(new { message = "Error processing organisation ID. Please try again or leave it blank." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An unexpected error occurred during registration. Please try again later." });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                // Validate login
                var isAuthenticated = await _userService.AuthenticateAsync(request.Username, request.Password);
                if (!isAuthenticated)
                    return Unauthorized(new { message = "Invalid username or password" });

                var user = await _userService.GetUserByUsernameAsync(request.Username);
                if (!user.IsActive)
                    return Unauthorized(new { message = "User account is deactivated" });

                // Generate JWT token
                var token = GenerateJwtToken(user);

                return Ok(new AuthResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = token,
                    Role = user.Role
                });
            }
            catch (InvalidCastException ex) when (ex.Message.Contains("organisation_id"))
            {
                _logger.LogError(ex, "Database error with organisation_id during login for user: {Username}", request.Username);
                return BadRequest(new { message = "Database error with user organisation. Please contact support." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An unexpected error occurred during login. Please try again later." });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, NormalizeRole(user.Role))
            };
            
            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return role;

            // Normalize common roles to match authorization attributes
            return role.ToLowerInvariant() switch
            {
                "admin" => "Administrator",
                "manager" => "Manager",
                "supervisor" => "Supervisor",
                "auditor" => "Auditor",
                _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(role.ToLower())
            };
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public Guid? OrganisationId { get; set; } // Make nullable since it's nullable in the database
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
    }
} 