using AuditSystem.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AuditSystem.Domain.Repositories;

namespace AuditSystem.Services
{
    public class RabbitMQNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQNotificationService> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private const string ExchangeName = "notification_exchange";
        private const string QueueName = "notification_queue";
        private const string RoutingKey = "notification";

        public RabbitMQNotificationService(
            IServiceProvider serviceProvider,
            ILogger<RabbitMQNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Configure RabbitMQ connection
            _connectionFactory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RabbitMQ__VirtualHost") ?? "/"
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await InitializeRabbitMQAsync();
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RabbitMQ notification service");
            }
            finally
            {
                CleanupRabbitMQ();
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);

            // Declare queue
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

            // Bind queue to exchange
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);

            _logger.LogInformation("RabbitMQ notification service initialized");
        }

        private async Task ProcessNotificationsAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    await ProcessNotificationMessageAsync(message);
                    
                    // Acknowledge the message
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification message");
                    // Reject the message and requeue it
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessNotificationMessageAsync(string message)
        {
            try
            {
                var notificationData = JsonSerializer.Deserialize<NotificationMessage>(message);
                
                using var scope = _serviceProvider.CreateScope();
                var notificationRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.INotificationRepository>();

                // Create notification from message
                var notification = new Domain.Entities.Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = notificationData.UserId,
                    OrganisationId = notificationData.OrganisationId,
                    Type = notificationData.Type,
                    Title = notificationData.Title,
                    Message = notificationData.Message,
                    Priority = notificationData.Priority,
                    Channel = "in_app",
                    Status = "sent", // Mark as sent immediately since we're broadcasting via SignalR
                    CreatedAt = DateTime.UtcNow,
                    SentAt = DateTime.UtcNow
                };

                await notificationRepository.AddAsync(notification);
                await notificationRepository.SaveChangesAsync();
                
                _logger.LogInformation("Processed notification message for user {UserId}", notificationData.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification message: {Message}", message);
            }
        }

        public static async Task PublishNotificationAsync(IModel channel, NotificationMessage notification)
        {
            var message = JsonSerializer.Serialize(notification);
            var body = Encoding.UTF8.GetBytes(message);
            
            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: null,
                body: body);
        }

        private void CleanupRabbitMQ()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("RabbitMQ notification service cleaned up");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            CleanupRabbitMQ();
            await base.StopAsync(cancellationToken);
        }
    }
} 