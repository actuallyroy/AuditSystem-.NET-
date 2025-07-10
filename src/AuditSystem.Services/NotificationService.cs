using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AuditSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IOrganisationRepository _organisationRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConnection _rabbitMqConnection;
        private readonly IModel _rabbitMqChannel;
        private const string ExchangeName = "notification_exchange";
        private const string RoutingKey = "notification";

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationTemplateRepository templateRepository,
            IUserRepository userRepository,
            IAuditRepository auditRepository,
            IOrganisationRepository organisationRepository,
            IAssignmentRepository assignmentRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _templateRepository = templateRepository;
            _userRepository = userRepository;
            _auditRepository = auditRepository;
            _organisationRepository = organisationRepository;
            _assignmentRepository = assignmentRepository;
            _logger = logger;

            // Initialize RabbitMQ connection
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RabbitMQ__VirtualHost") ?? "/"
            };

            try
            {
                _rabbitMqConnection = factory.CreateConnection();
                _rabbitMqChannel = _rabbitMqConnection.CreateModel();
                _rabbitMqChannel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
                _logger.LogInformation("RabbitMQ connection established for notification service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                // Continue without RabbitMQ - notifications will be created directly in database
            }
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.NotificationId = Guid.NewGuid();
                notification.CreatedAt = DateTime.UtcNow;
                notification.Status = "pending";

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();

                _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                    notification.NotificationId, notification.UserId);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for user {UserId}", notification.UserId);
                throw;
            }
        }

        public async Task<Notification> CreateNotificationFromTemplateAsync(string templateName, Dictionary<string, object> placeholders, Guid? userId = null, Guid? organisationId = null)
        {
            try
            {
                var template = await _templateRepository.GetByNameAsync(templateName);
                if (template == null)
                {
                    throw new ArgumentException($"Template '{templateName}' not found");
                }

                var title = template.Subject;
                var message = template.Body;

                // Replace placeholders in title and message
                foreach (var placeholder in placeholders)
                {
                    title = title.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                    message = message.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                }

                // If in_app, strip HTML
                if (template.Channel == "in_app")
                {
                    title = StripHtmlTags(title);
                    message = StripHtmlTags(message);
                }

                // Remove any unresolved placeholders and log a warning if found
                if (Regex.IsMatch(title, "{[^}]+}"))
                {
                    _logger.LogWarning($"Unresolved placeholders found in notification title: {title}");
                    title = RemoveUnresolvedPlaceholders(title);
                }
                if (Regex.IsMatch(message, "{[^}]+}"))
                {
                    _logger.LogWarning($"Unresolved placeholders found in notification message: {message}");
                    message = RemoveUnresolvedPlaceholders(message);
                }

                var notification = new Notification
                {
                    UserId = userId,
                    OrganisationId = organisationId,
                    Type = template.Type,
                    Title = title,
                    Message = message,
                    Channel = template.Channel,
                    Priority = "medium",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                return await CreateNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification from template {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepository.GetNotificationsByUserAsync(userId, page, pageSize);
        }

        public async Task<IEnumerable<Notification>> GetOrganisationNotificationsAsync(Guid organisationId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepository.GetNotificationsByOrganisationAsync(organisationId, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountByUserAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                return await _notificationRepository.MarkAsReadAsync(notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                return await _notificationRepository.MarkAllAsReadByUserAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                return await _notificationRepository.DeleteNotificationAsync(notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<bool> SendAuditAssignmentNotificationAsync(Guid auditId, Guid userId, Guid organisationId)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" },
                    { "due_date", audit.Assignment?.DueDate?.ToString("MMM dd, yyyy") ?? "TBD" }
                };

                await SendNotificationViaRabbitMQ("assignment_notification", placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit assignment notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendAssignmentNotificationAsync(Guid assignmentId, Guid userId, Guid organisationId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (assignment == null || user == null || organisation == null)
                {
                    return false;
                }

                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "template_name", assignment.Template?.Name ?? "Unknown Template" },
                    { "due_date", assignment.DueDate?.ToString("MMM dd, yyyy") ?? "TBD" },
                    { "priority", assignment.Priority ?? "medium" }
                };

                await SendNotificationViaRabbitMQ("assignment_notification", placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send assignment notification for assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<bool> SendAuditCompletedNotificationAsync(Guid auditId, Guid userId, Guid organisationId)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" }
                };

                await SendNotificationViaRabbitMQ("audit_completed_notification", placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit completed notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendAuditReviewedNotificationAsync(Guid auditId, Guid userId, Guid organisationId, string status, decimal? score = null)
        {
            try
            {
                var audit = await _auditRepository.GetByIdAsync(auditId);
                var user = await _userRepository.GetByIdAsync(userId);
                var organisation = await _organisationRepository.GetByIdAsync(organisationId);

                if (audit == null || user == null || organisation == null)
                {
                    return false;
                }

                string templateName = status.ToLower() == "approved" ? "audit_approved_notification" : "audit_rejected_notification";
                var placeholders = new Dictionary<string, object>
                {
                    { "user_name", $"{user.FirstName} {user.LastName}" },
                    { "store_name", audit.StoreInfo?.RootElement.GetProperty("name").GetString() ?? "Unknown Store" }
                };

                if (status.ToLower() == "approved" && score.HasValue)
                {
                    placeholders.Add("score", score.Value.ToString("F1"));
                    placeholders.Add("critical_issues", audit.CriticalIssues.ToString());
                }
                else if (status.ToLower() == "rejected")
                {
                    placeholders.Add("reason", audit.ManagerNotes ?? "No specific reason provided");
                }

                await SendNotificationViaRabbitMQ(templateName, placeholders, userId, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit reviewed notification for audit {AuditId}", auditId);
                return false;
            }
        }

        public async Task<bool> SendSystemAlertAsync(string title, string message, Guid? organisationId = null, string priority = "medium")
        {
            try
            {
                var placeholders = new Dictionary<string, object>
                {
                    { "message", message }
                };

                await SendNotificationViaRabbitMQ("system_notification", placeholders, null, organisationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system alert: {Title}", title);
                return false;
            }
        }

        public async Task<bool> SendBulkNotificationAsync(string title, string message, List<Guid> userIds, Guid? organisationId = null)
        {
            try
            {
                var notifications = new List<Notification>();
                foreach (var userId in userIds)
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        OrganisationId = organisationId,
                        Type = "system_alert",
                        Title = title,
                        Message = message,
                        Channel = "in_app",
                        Priority = "medium",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    notifications.Add(notification);
                }

                await _notificationRepository.AddRangeAsync(notifications);
                await _notificationRepository.SaveChangesAsync();

                _logger.LogInformation("Sent bulk notification to {Count} users", userIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification to {Count} users", userIds.Count);
                return false;
            }
        }

        public async Task<bool> ProcessNotificationDeliveryAsync()
        {
            try
            {
                // Get pending notifications that should be sent via SignalR
                var pendingNotifications = await _notificationRepository.FindAsync(n => n.Status == "pending" && n.Channel == "in_app");
                
                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        // Mark notification as ready for sending
                        notification.Status = "sent";
                        notification.SentAt = DateTime.UtcNow;
                        _notificationRepository.Update(notification);
                        
                        _logger.LogInformation("Marked notification {NotificationId} as sent via SignalR", 
                            notification.NotificationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process notification {NotificationId}", notification.NotificationId);
                        notification.Status = "failed";
                        notification.ErrorMessage = ex.Message;
                        notification.RetryCount++;
                        _notificationRepository.Update(notification);
                    }
                }

                await _notificationRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification delivery");
                return false;
            }
        }

        public async Task<bool> CleanupExpiredNotificationsAsync()
        {
            try
            {
                return await _notificationRepository.DeleteExpiredNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired notifications");
                return false;
            }
        }

        public async Task<IEnumerable<Notification>> GetReadyToSendNotificationsAsync()
        {
            try
            {
                var readyNotifications = await _notificationRepository.FindAsync(n => n.Status == "sent");
                return readyNotifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ready to send notifications");
                return new List<Notification>();
            }
        }

        public async Task<bool> MarkNotificationAsSentAsync(Guid notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification != null)
                {
                    notification.Status = "sent";
                    notification.SentAt = DateTime.UtcNow;
                    _notificationRepository.Update(notification);
                    await _notificationRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("Marked notification {NotificationId} as sent", notificationId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as sent", notificationId);
                return false;
            }
        }

        public async Task<bool> MarkNotificationAsBroadcastedAsync(Guid notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification != null)
                {
                    notification.Status = "broadcasted";
                    _notificationRepository.Update(notification);
                    await _notificationRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("Marked notification {NotificationId} as broadcasted", notificationId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as broadcasted", notificationId);
                return false;
            }
        }

        public async Task<bool> MarkNotificationAsDeliveredAsync(Guid notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification != null)
                {
                    notification.Status = "delivered";
                    notification.DeliveredAt = DateTime.UtcNow;
                    _notificationRepository.Update(notification);
                    await _notificationRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("Marked notification {NotificationId} as delivered", notificationId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as delivered", notificationId);
                return false;
            }
        }

        public async Task<bool> MarkNotificationAsFailedAsync(Guid notificationId, string errorMessage)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification != null)
                {
                    notification.Status = "failed";
                    notification.ErrorMessage = errorMessage;
                    notification.RetryCount++;
                    _notificationRepository.Update(notification);
                    await _notificationRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("Marked notification {NotificationId} as failed: {ErrorMessage}", notificationId, errorMessage);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as failed", notificationId);
                return false;
            }
        }

        private async Task SendNotificationViaRabbitMQ(string templateName, Dictionary<string, object> placeholders, Guid? userId = null, Guid? organisationId = null)
        {
            try
            {
                if (_rabbitMqChannel == null || _rabbitMqChannel.IsClosed)
                {
                    // Fallback to direct database creation if RabbitMQ is not available
                    await CreateNotificationFromTemplateAsync(templateName, placeholders, userId, organisationId);
                    return;
                }

                var template = await _templateRepository.GetByNameAsync(templateName);
                if (template == null)
                {
                    throw new ArgumentException($"Template '{templateName}' not found");
                }

                var title = template.Subject;
                var message = template.Body;

                // Replace placeholders in title and message
                foreach (var placeholder in placeholders)
                {
                    title = title.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                    message = message.Replace($"{{{placeholder.Key}}}", placeholder.Value?.ToString() ?? "");
                }

                // If in_app, strip HTML
                if (template.Channel == "in_app")
                {
                    title = StripHtmlTags(title);
                    message = StripHtmlTags(message);
                }

                // Remove any unresolved placeholders and log a warning if found
                if (Regex.IsMatch(title, "{[^}]+}"))
                {
                    _logger.LogWarning($"Unresolved placeholders found in notification title: {title}");
                    title = RemoveUnresolvedPlaceholders(title);
                }
                if (Regex.IsMatch(message, "{[^}]+}"))
                {
                    _logger.LogWarning($"Unresolved placeholders found in notification message: {message}");
                    message = RemoveUnresolvedPlaceholders(message);
                }

                var notificationMessage = new NotificationMessage
                {
                    UserId = userId,
                    OrganisationId = organisationId,
                    Type = template.Type,
                    Title = title,
                    Message = message,
                    Priority = "medium"
                };

                var messageJson = JsonSerializer.Serialize(notificationMessage);
                var body = System.Text.Encoding.UTF8.GetBytes(messageJson);
                
                _rabbitMqChannel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    basicProperties: null,
                    body: body);

                _logger.LogInformation("Published notification message to RabbitMQ for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification via RabbitMQ, falling back to direct creation");
                // Fallback to direct database creation
                await CreateNotificationFromTemplateAsync(templateName, placeholders, userId, organisationId);
            }
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        private string RemoveUnresolvedPlaceholders(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Remove any {placeholder} patterns
            return Regex.Replace(input, "{[^}]+}", string.Empty);
        }

        public void Dispose()
        {
            _rabbitMqChannel?.Close();
            _rabbitMqConnection?.Close();
        }
    }

    public class NotificationMessage
    {
        public Guid? UserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium";
    }
} 