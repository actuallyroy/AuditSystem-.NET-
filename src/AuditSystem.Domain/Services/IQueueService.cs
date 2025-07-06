using AuditSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AuditSystem.Domain.Services
{
    public interface IQueueService
    {
        Task InitializeAsync();
        Task PublishEmailNotificationAsync(EmailNotificationMessage message);
        Task PublishSmsNotificationAsync(SmsNotificationMessage message);
        Task PublishPushNotificationAsync(PushNotificationMessage message);
        Task PublishNotificationAsync(NotificationMessage message);
        Task<bool> IsHealthyAsync();
        Task CloseAsync();
    }

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailNotificationMessage message);
        Task<bool> SendBulkEmailAsync(IEnumerable<EmailNotificationMessage> messages);
    }

    public interface ISmsService
    {
        Task<bool> SendSmsAsync(SmsNotificationMessage message);
        Task<bool> SendBulkSmsAsync(IEnumerable<SmsNotificationMessage> messages);
    }

    public interface IPushNotificationService
    {
        Task<bool> SendPushNotificationAsync(PushNotificationMessage message);
        Task<bool> SendBulkPushNotificationAsync(IEnumerable<PushNotificationMessage> messages);
    }
} 