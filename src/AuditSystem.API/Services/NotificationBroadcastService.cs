using AuditSystem.Domain.Services;
using AuditSystem.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuditSystem.API.Services
{
    public class NotificationBroadcastService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBroadcastService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds

        public NotificationBroadcastService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBroadcastService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Broadcast Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await BroadcastReadyNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification broadcast service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Broadcast Service stopped");
        }

        private async Task BroadcastReadyNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            try
            {
                // Get notifications that are ready to be sent (status = "sent")
                // These will be broadcasted and then marked as "broadcasted" until client acknowledges
                var readyNotifications = await notificationService.GetReadyToSendNotificationsAsync();

                foreach (var notification in readyNotifications)
                {
                    try
                    {
                        // Create notification object for SignalR
                        var notificationData = new
                        {
                            notificationId = notification.NotificationId,
                            type = notification.Type,
                            title = notification.Title,
                            message = notification.Message,
                            priority = notification.Priority,
                            timestamp = notification.CreatedAt,
                            userId = notification.UserId,
                            organisationId = notification.OrganisationId
                        };

                        // Send notification via SignalR
                        if (notification.UserId.HasValue)
                        {
                            await NotificationHub.SendNotificationToUser(hubContext, notification.UserId.Value, notificationData);
                            _logger.LogInformation("Broadcasted notification {NotificationId} to user {UserId} via SignalR, waiting for acknowledgment", 
                                notification.NotificationId, notification.UserId);
                        }
                        else if (notification.OrganisationId.HasValue)
                        {
                            await NotificationHub.SendNotificationToOrganisation(hubContext, notification.OrganisationId.Value, notificationData);
                            _logger.LogInformation("Broadcasted notification {NotificationId} to organisation {OrganisationId} via SignalR, waiting for acknowledgment", 
                                notification.NotificationId, notification.OrganisationId);
                        }

                        // Mark notification as broadcasted (not delivered yet - waiting for client acknowledgment)
                        await notificationService.MarkNotificationAsBroadcastedAsync(notification.NotificationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to broadcast notification {NotificationId} via SignalR", notification.NotificationId);
                        await notificationService.MarkNotificationAsFailedAsync(notification.NotificationId, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BroadcastReadyNotificationsAsync");
            }
        }
    }
} 