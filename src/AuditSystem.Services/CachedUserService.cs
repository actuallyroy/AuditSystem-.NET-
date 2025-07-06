using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class CachedUserService : IUserService
    {
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedUserService> _logger;

        public CachedUserService(
            IUserService userService,
            ICacheService cacheService,
            ILogger<CachedUserService> logger)
        {
            _userService = userService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            var cacheKey = CacheKeys.UserById(userId);
            
            var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
            if (cachedUser != null)
            {
                _logger.LogDebug("User {UserId} found in cache", userId);
                return cachedUser;
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null)
            {
                await _cacheService.SetAsync(cacheKey, user, CacheKeys.UserCacheExpiration);
                _logger.LogDebug("User {UserId} cached for {Expiration} minutes", userId, CacheKeys.UserCacheExpiration.TotalMinutes);
            }

            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var cacheKey = CacheKeys.UserByUsername(username);
            
            var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
            if (cachedUser != null)
            {
                _logger.LogDebug("User {Username} found in cache", username);
                return cachedUser;
            }

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user != null)
            {
                // Cache by both username and ID
                await _cacheService.SetAsync(cacheKey, user, CacheKeys.UserCacheExpiration);
                await _cacheService.SetAsync(CacheKeys.UserById(user.UserId), user, CacheKeys.UserCacheExpiration);
                _logger.LogDebug("User {Username} cached for {Expiration} minutes", username, CacheKeys.UserCacheExpiration.TotalMinutes);
            }

            return user;
        }

        public async Task<IEnumerable<User>> GetUsersByOrganisationAsync(Guid organisationId)
        {
            var cacheKey = CacheKeys.UsersByOrganization(organisationId);
            
            var cachedUsers = await _cacheService.GetAsync<List<User>>(cacheKey);
            if (cachedUsers != null)
            {
                _logger.LogDebug("Users for organization {OrganisationId} found in cache", organisationId);
                return cachedUsers;
            }

            var users = await _userService.GetUsersByOrganisationAsync(organisationId);
            var usersList = users.ToList();

            if (usersList.Any())
            {
                await _cacheService.SetAsync(cacheKey, usersList, CacheKeys.UserCacheExpiration);
                
                // Also cache individual users
                foreach (var user in usersList)
                {
                    await _cacheService.SetAsync(CacheKeys.UserById(user.UserId), user, CacheKeys.UserCacheExpiration);
                }
                
                _logger.LogDebug("Users for organization {OrganisationId} cached for {Expiration} minutes", 
                    organisationId, CacheKeys.UserCacheExpiration.TotalMinutes);
            }

            return usersList;
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            var cacheKey = CacheKeys.UsersByRole(role);
            
            var cachedUsers = await _cacheService.GetAsync<List<User>>(cacheKey);
            if (cachedUsers != null)
            {
                _logger.LogDebug("Users with role {Role} found in cache", role);
                return cachedUsers;
            }

            var users = await _userService.GetUsersByRoleAsync(role);
            var usersList = users.ToList();

            if (usersList.Any())
            {
                await _cacheService.SetAsync(cacheKey, usersList, CacheKeys.UserCacheExpiration);
                _logger.LogDebug("Users with role {Role} cached for {Expiration} minutes", 
                    role, CacheKeys.UserCacheExpiration.TotalMinutes);
            }

            return usersList;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            // For performance reasons, we don't cache all users by default
            // This method is typically used for admin functions and should be current
            return await _userService.GetAllUsersAsync();
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            var createdUser = await _userService.CreateUserAsync(user, password);
            
            // Cache the new user
            await _cacheService.SetAsync(CacheKeys.UserById(createdUser.UserId), createdUser, CacheKeys.UserCacheExpiration);
            await _cacheService.SetAsync(CacheKeys.UserByUsername(createdUser.Username), createdUser, CacheKeys.UserCacheExpiration);
            
            // Invalidate organization users cache
            if (createdUser.OrganisationId.HasValue)
            {
                await _cacheService.RemoveAsync(CacheKeys.UsersByOrganization(createdUser.OrganisationId.Value));
            }
            
            // Invalidate role users cache
            await _cacheService.RemoveAsync(CacheKeys.UsersByRole(createdUser.Role));
            
            _logger.LogDebug("User {UserId} created and cached", createdUser.UserId);
            return createdUser;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var updatedUser = await _userService.UpdateUserAsync(user);
            
            // Update cache
            await _cacheService.SetAsync(CacheKeys.UserById(updatedUser.UserId), updatedUser, CacheKeys.UserCacheExpiration);
            await _cacheService.SetAsync(CacheKeys.UserByUsername(updatedUser.Username), updatedUser, CacheKeys.UserCacheExpiration);
            
            // Invalidate related caches
            if (updatedUser.OrganisationId.HasValue)
            {
                await _cacheService.RemoveAsync(CacheKeys.UsersByOrganization(updatedUser.OrganisationId.Value));
            }
            await _cacheService.RemoveAsync(CacheKeys.UsersByRole(updatedUser.Role));
            
            _logger.LogDebug("User {UserId} updated and cache refreshed", updatedUser.UserId);
            return updatedUser;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var result = await _userService.DeactivateUserAsync(userId);
            
            if (result)
            {
                // Remove from cache
                await _cacheService.RemoveByPatternAsync(CacheKeys.UserPattern(userId));
                _logger.LogDebug("User {UserId} deactivated and removed from cache", userId);
            }
            
            return result;
        }

        public async Task<bool> UpdateUserPasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var result = await _userService.UpdateUserPasswordAsync(userId, currentPassword, newPassword);
            
            if (result)
            {
                // Password update doesn't change user data, so we don't need to update cache
                _logger.LogDebug("Password updated for user {UserId}", userId);
            }
            
            return result;
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            // Authentication should not be cached for security reasons
            return await _userService.AuthenticateAsync(username, password);
        }

        // Helper method to invalidate all user-related caches
        public async Task InvalidateUserCacheAsync(Guid userId)
        {
            await _cacheService.RemoveByPatternAsync(CacheKeys.UserPattern(userId));
            _logger.LogDebug("All cache entries for user {UserId} invalidated", userId);
        }

        // Helper method to invalidate organization-related user caches
        public async Task InvalidateOrganizationUserCacheAsync(Guid organizationId)
        {
            await _cacheService.RemoveAsync(CacheKeys.UsersByOrganization(organizationId));
            _logger.LogDebug("Organization user cache invalidated for organization {OrganizationId}", organizationId);
        }

        // Helper method to warm up user cache
        public async Task WarmUpUserCacheAsync(Guid userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null)
            {
                await _cacheService.SetAsync(CacheKeys.UserById(userId), user, CacheKeys.UserCacheExpiration);
                await _cacheService.SetAsync(CacheKeys.UserByUsername(user.Username), user, CacheKeys.UserCacheExpiration);
                _logger.LogDebug("User cache warmed up for user {UserId}", userId);
            }
        }
    }
} 