using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class NullQueueService : IQueueService
    {
        private readonly ILogger<NullQueueService> _logger;

        public NullQueueService(ILogger<NullQueueService> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync()
        {
            _logger.LogInformation("NullQueueService initialized - no actual queue processing");
            return Task.CompletedTask;
        }

        public Task PublishEmailNotificationAsync(EmailNotificationMessage message)
        {
            _logger.LogInformation("Email notification would be published: {Subject}", message.Subject);
            return Task.CompletedTask;
        }

        public Task PublishSmsNotificationAsync(SmsNotificationMessage message)
        {
            _logger.LogInformation("SMS notification would be published: {Body}", message.Body);
            return Task.CompletedTask;
        }

        public Task PublishPushNotificationAsync(PushNotificationMessage message)
        {
            _logger.LogInformation("Push notification would be published: {Title}", message.Subject);
            return Task.CompletedTask;
        }

        public Task PublishNotificationAsync(NotificationMessage message)
        {
            _logger.LogInformation("Notification would be published: {Type} - {Subject}", message.Type, message.Subject);
            return Task.CompletedTask;
        }

        public Task<bool> IsHealthyAsync()
        {
            return Task.FromResult(true);
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }
    }
} 