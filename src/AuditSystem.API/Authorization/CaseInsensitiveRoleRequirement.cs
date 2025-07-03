using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuditSystem.API.Authorization
{
    public class CaseInsensitiveRoleRequirement : IAuthorizationRequirement
    {
        public string[] Roles { get; }

        public CaseInsensitiveRoleRequirement(params string[] roles)
        {
            Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }
    }

    public class CaseInsensitiveRoleHandler : AuthorizationHandler<CaseInsensitiveRoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CaseInsensitiveRoleRequirement requirement)
        {
            // Try to find the user's role from different possible claim types
            var possibleRoleClaims = new[]
            {
                "role",
                ClaimTypes.Role,
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                "roles"
            };

            string userRole = null;
            
            // Try each possible claim type
            foreach (var claimType in possibleRoleClaims)
            {
                var roleClaim = context.User.FindFirst(claimType);
                if (roleClaim != null)
                {
                    userRole = roleClaim.Value;
                    break;
                }
            }

            // Also check if user has multiple role claims
            if (string.IsNullOrEmpty(userRole))
            {
                var roleClaims = context.User.Claims
                    .Where(c => possibleRoleClaims.Contains(c.Type))
                    .Select(c => c.Value)
                    .ToList();

                if (roleClaims.Any())
                {
                    // Check if any of the user's roles match the required roles
                    foreach (var role in roleClaims)
                    {
                        foreach (var requiredRole in requirement.Roles)
                        {
                            if (string.Equals(role, requiredRole, StringComparison.OrdinalIgnoreCase))
                            {
                                context.Succeed(requirement);
                                return Task.CompletedTask;
                            }
                        }
                    }
                }
            }
            else
            {
                // Check single role claim
                foreach (var requiredRole in requirement.Roles)
                {
                    if (string.Equals(userRole, requiredRole, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }

            // Log for debugging (optional)
            var allClaims = string.Join(", ", context.User.Claims.Select(c => $"{c.Type}:{c.Value}"));
            Console.WriteLine($"[DEBUG] Authorization failed. User claims: {allClaims}");
            Console.WriteLine($"[DEBUG] Required roles: {string.Join(", ", requirement.Roles)}");

            // If no role matches, the requirement fails
            return Task.CompletedTask;
        }
    }
} 