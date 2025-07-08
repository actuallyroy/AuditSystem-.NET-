using AuditSystem.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public NotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification background service started");

            var processingTimer = new Timer(async _ => await ProcessNotificationsAsync(), null, TimeSpan.Zero, _processingInterval);
            var cleanupTimer = new Timer(async _ => await CleanupNotificationsAsync(), null, TimeSpan.Zero, _cleanupInterval);

            try
            {
                // Keep the service running until cancellation is requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            finally
            {
                processingTimer.Dispose();
                cleanupTimer.Dispose();
            }
        }

        private async Task ProcessNotificationsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.ProcessNotificationDeliveryAsync();
                _logger.LogDebug("Notification delivery processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification delivery");
            }
        }

        private async Task CleanupNotificationsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.CleanupExpiredNotificationsAsync();
                _logger.LogDebug("Notification cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired notifications");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification background service stopped");
            await base.StopAsync(cancellationToken);
        }
    }
} 