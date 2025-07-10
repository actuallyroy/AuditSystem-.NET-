using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuditSystem.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;
        private static readonly ConcurrentDictionary<string, UserConnection> _userConnections = new();

        public NotificationHub(
            INotificationService notificationService,
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var organisationId = GetCurrentUserOrganizationId();

                if (userId.HasValue)
                {
                    var userConnection = new UserConnection
                    {
                        UserId = userId.Value,
                        ConnectionId = Context.ConnectionId,
                        Role = userRole,
                        OrganisationId = organisationId,
                        ConnectedAt = DateTime.UtcNow
                    };

                    _userConnections.TryAdd(Context.ConnectionId, userConnection);

                    _logger.LogInformation("User {UserId} connected to SignalR hub with connection {ConnectionId}", 
                        userId, Context.ConnectionId);

                    // Send heartbeat to confirm connection (without unread count)
                    await Clients.Caller.SendAsync("Heartbeat", new { timestamp = DateTime.UtcNow });

                    // Send initial unread notification count only once on connection
                    var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
                    await Clients.Caller.SendAsync("UnreadCount", unreadCount);
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                _userConnections.TryRemove(Context.ConnectionId, out _);

                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    _logger.LogInformation("User {UserId} disconnected from SignalR hub", userId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
            }
        }

        public async Task SubscribeToUser(string userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || currentUserId.Value.ToString() != userId)
                {
                    _logger.LogWarning("User {CurrentUserId} attempted to subscribe to notifications for user {UserId}", 
                        currentUserId, userId);
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} subscribed to their notifications", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to notifications", userId);
            }
        }

        public async Task JoinOrganisation(string organisationId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserOrgId = GetCurrentUserOrganizationId();

                if (!currentUserId.HasValue || currentUserOrgId?.ToString() != organisationId)
                {
                    _logger.LogWarning("User {CurrentUserId} attempted to join organisation {OrganisationId} without permission", 
                        currentUserId, organisationId);
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{organisationId}");
                _logger.LogInformation("User {UserId} joined organisation {OrganisationId} group", currentUserId, organisationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining organisation {OrganisationId}", organisationId);
            }
        }

        public async Task LeaveOrganisation(string organisationId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org_{organisationId}");
                
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} left organisation {OrganisationId} group", currentUserId, organisationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving organisation {OrganisationId}", organisationId);
            }
        }

        public async Task SendTestMessage(string message)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return;
                }

                var testNotification = new
                {
                    type = "test",
                    title = "Test Message",
                    message = message,
                    timestamp = DateTime.UtcNow,
                    userId = currentUserId.Value
                };

                await Clients.Caller.SendAsync("ReceiveNotification", testNotification);
                _logger.LogInformation("Test message sent to user {UserId}: {Message}", currentUserId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test message");
            }
        }

        public async Task MarkNotificationAsRead(string notificationId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || !Guid.TryParse(notificationId, out var notificationGuid))
                {
                    return;
                }

                var success = await _notificationService.MarkAsReadAsync(notificationGuid, currentUserId.Value);
                if (success)
                {
                    // Update unread count for the user
                    var unreadCount = await _notificationService.GetUnreadCountAsync(currentUserId.Value);
                    await Clients.Caller.SendAsync("UnreadCount", unreadCount);
                    
                    // Send confirmation
                    await Clients.Caller.SendAsync("NotificationMarkedAsRead", new { 
                        notificationId = notificationId,
                        unreadCount = unreadCount
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            }
        }

        public async Task MarkAllNotificationsAsRead()
        {
            Guid? currentUserId = null;
            try
            {
                currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return;
                }

                var success = await _notificationService.MarkAllAsReadAsync(currentUserId.Value);
                if (success)
                {
                    await Clients.Caller.SendAsync("UnreadCount", 0);
                    
                    // Send confirmation
                    await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead", new { 
                        unreadCount = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", currentUserId);
            }
        }

        public async Task AcknowledgeDelivery(string notificationId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || !Guid.TryParse(notificationId, out var notificationGuid))
                {
                    _logger.LogWarning("Invalid acknowledgment request from user {UserId} for notification {NotificationId}", 
                        currentUserId, notificationId);
                    return;
                }

                // Mark notification as delivered in the database
                var success = await _notificationService.MarkNotificationAsDeliveredAsync(notificationGuid);
                if (success)
                {
                    // Send acknowledgment confirmation to the client
                    await Clients.Caller.SendAsync("DeliveryAcknowledged", new { 
                        notificationId = notificationId,
                        acknowledgedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("Delivery acknowledged for notification {NotificationId} by user {UserId}", 
                        notificationId, currentUserId);
                }
                else
                {
                    _logger.LogWarning("Failed to mark notification {NotificationId} as delivered for user {UserId}", 
                        notificationId, currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging delivery for notification {NotificationId} by user {UserId}", 
                    notificationId, GetCurrentUserId());
            }
        }

        // Static methods for sending notifications to connected clients
        public static async Task SendNotificationToUser(IHubContext<NotificationHub> hubContext, Guid userId, object notification)
        {
            await hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
        }

        public static async Task SendNotificationToOrganisation(IHubContext<NotificationHub> hubContext, Guid organisationId, object notification)
        {
            await hubContext.Clients.Group($"org_{organisationId}").SendAsync("ReceiveNotification", notification);
        }

        public static async Task SendNotificationToAll(IHubContext<NotificationHub> hubContext, object notification)
        {
            await hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }

        public static async Task UpdateUnreadCount(IHubContext<NotificationHub> hubContext, Guid userId, int count)
        {
            await hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCount", count);
        }

        public static async Task SendHeartbeat(IHubContext<NotificationHub> hubContext)
        {
            await hubContext.Clients.All.SendAsync("Heartbeat", new { timestamp = DateTime.UtcNow });
        }

        // Helper methods
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "user";
        }

        private Guid? GetCurrentUserOrganizationId()
        {
            var orgIdClaim = Context.User?.FindFirst("OrganisationId")?.Value;
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
        }
    }

    public class UserConnection
    {
        public Guid UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? OrganisationId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
} 