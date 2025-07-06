using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class RabbitMQQueueService : IQueueService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly NotificationQueueConfig _config;
        private readonly ILogger<RabbitMQQueueService> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IPushNotificationService _pushService;

        public RabbitMQQueueService(
            IOptions<NotificationQueueConfig> config,
            ILogger<RabbitMQQueueService> logger,
            IEmailService emailService,
            ISmsService smsService,
            IPushNotificationService pushService)
        {
            _config = config.Value;
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;

            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Declare exchange
                _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

                // Declare queues
                _channel.QueueDeclare(_config.EmailQueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueDeclare(_config.SmsQueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueDeclare(_config.PushQueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueDeclare(_config.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);

                // Bind queues to exchange
                _channel.QueueBind(_config.EmailQueueName, _config.ExchangeName, _config.EmailRoutingKey);
                _channel.QueueBind(_config.SmsQueueName, _config.ExchangeName, _config.SmsRoutingKey);
                _channel.QueueBind(_config.PushQueueName, _config.ExchangeName, _config.PushRoutingKey);

                // Set up consumers
                SetupEmailConsumer();
                SetupSmsConsumer();
                SetupPushConsumer();

                _logger.LogInformation("RabbitMQ queue service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ queue service");
                throw;
            }
        }

        public async Task PublishEmailNotificationAsync(EmailNotificationMessage message)
        {
            await PublishMessageAsync(message, _config.EmailRoutingKey);
        }

        public async Task PublishSmsNotificationAsync(SmsNotificationMessage message)
        {
            await PublishMessageAsync(message, _config.SmsRoutingKey);
        }

        public async Task PublishPushNotificationAsync(PushNotificationMessage message)
        {
            await PublishMessageAsync(message, _config.PushRoutingKey);
        }

        public async Task PublishNotificationAsync(NotificationMessage message)
        {
            var routingKey = message.Channel switch
            {
                "email" => _config.EmailRoutingKey,
                "sms" => _config.SmsRoutingKey,
                "push" => _config.PushRoutingKey,
                _ => _config.EmailRoutingKey // Default to email
            };

            await PublishMessageAsync(message, routingKey);
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                return _connection.IsOpen && _channel.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        public async Task CloseAsync()
        {
            _channel?.Close();
            _connection?.Close();
        }

        private async Task PublishMessageAsync(object message, string routingKey)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: _config.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published message {MessageId} to routing key {RoutingKey}", 
                    properties.MessageId, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to routing key {RoutingKey}", routingKey);
                throw;
            }
        }

        private void SetupEmailConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var emailMessage = JsonConvert.DeserializeObject<EmailNotificationMessage>(message);

                    _logger.LogInformation("Processing email notification {MessageId}", emailMessage.MessageId);

                    var success = await _emailService.SendEmailAsync(emailMessage);

                    if (success)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Email notification {MessageId} sent successfully", emailMessage.MessageId);
                    }
                    else
                    {
                        HandleFailedMessage(ea, emailMessage.MessageId.ToString(), "Email sending failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email notification");
                    HandleFailedMessage(ea, "unknown", ex.Message);
                }
            };

            _channel.BasicConsume(queue: _config.EmailQueueName, autoAck: false, consumer: consumer);
        }

        private void SetupSmsConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var smsMessage = JsonConvert.DeserializeObject<SmsNotificationMessage>(message);

                    _logger.LogInformation("Processing SMS notification {MessageId}", smsMessage.MessageId);

                    var success = await _smsService.SendSmsAsync(smsMessage);

                    if (success)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("SMS notification {MessageId} sent successfully", smsMessage.MessageId);
                    }
                    else
                    {
                        HandleFailedMessage(ea, smsMessage.MessageId.ToString(), "SMS sending failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SMS notification");
                    HandleFailedMessage(ea, "unknown", ex.Message);
                }
            };

            _channel.BasicConsume(queue: _config.SmsQueueName, autoAck: false, consumer: consumer);
        }

        private void SetupPushConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var pushMessage = JsonConvert.DeserializeObject<PushNotificationMessage>(message);

                    _logger.LogInformation("Processing push notification {MessageId}", pushMessage.MessageId);

                    var success = await _pushService.SendPushNotificationAsync(pushMessage);

                    if (success)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Push notification {MessageId} sent successfully", pushMessage.MessageId);
                    }
                    else
                    {
                        HandleFailedMessage(ea, pushMessage.MessageId.ToString(), "Push notification sending failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing push notification");
                    HandleFailedMessage(ea, "unknown", ex.Message);
                }
            };

            _channel.BasicConsume(queue: _config.PushQueueName, autoAck: false, consumer: consumer);
        }

        private void HandleFailedMessage(BasicDeliverEventArgs ea, string messageId, string errorMessage)
        {
            var retryCount = GetRetryCount(ea.BasicProperties);
            
            if (retryCount < _config.MaxRetries)
            {
                // Reject and requeue with delay
                _channel.BasicNack(ea.DeliveryTag, false, true);
                _logger.LogWarning("Message {MessageId} failed, retry {RetryCount}/{MaxRetries}. Error: {Error}", 
                    messageId, retryCount + 1, _config.MaxRetries, errorMessage);
            }
            else
            {
                // Send to dead letter queue
                _channel.BasicNack(ea.DeliveryTag, false, false);
                _logger.LogError("Message {MessageId} failed after {MaxRetries} retries, sending to DLQ. Error: {Error}", 
                    messageId, _config.MaxRetries, errorMessage);
            }
        }

        private int GetRetryCount(IBasicProperties properties)
        {
            if (properties.Headers != null && properties.Headers.ContainsKey("x-retry-count"))
            {
                return Convert.ToInt32(properties.Headers["x-retry-count"]);
            }
            return 0;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 