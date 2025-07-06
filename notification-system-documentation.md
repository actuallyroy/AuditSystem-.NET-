# Notification System Documentation

## Overview

The Retail Execution Audit System now includes a comprehensive notification system that processes notifications through RabbitMQ queues. This system supports multiple notification channels (email, SMS, push notifications, and in-app notifications) with reliable message processing, retry mechanisms, and dead letter queues.

## Architecture

### Components

1. **Notification Entities** - Domain models for notifications and templates
2. **Notification Service** - Business logic for notification management
3. **RabbitMQ Queue Service** - Message queue processing
4. **Channel Services** - Email, SMS, and Push notification providers
5. **Repository Layer** - Data access for notifications
6. **API Controller** - REST endpoints for notification management

### Message Flow

```
User Action → Notification Service → RabbitMQ Queue → Channel Service → Recipient
```

## Database Schema

### Notification Table
```sql
CREATE TABLE notification (
    notification_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    organisation_id UUID REFERENCES organisation(organisation_id) ON DELETE CASCADE,
    type TEXT NOT NULL, -- 'assignment', 'audit_completed', 'audit_approved', 'audit_rejected', 'system'
    title TEXT NOT NULL,
    message TEXT NOT NULL,
    priority TEXT CHECK (priority IN ('low', 'medium', 'high', 'urgent')) NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMPTZ,
    channel TEXT CHECK (channel IN ('email', 'sms', 'push', 'in_app')) NOT NULL,
    status TEXT CHECK (status IN ('pending', 'sent', 'failed', 'delivered')) NOT NULL,
    retry_count INTEGER DEFAULT 0,
    sent_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    error_message TEXT,
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);
```

### Notification Template Table
```sql
CREATE TABLE notification_template (
    template_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name TEXT UNIQUE NOT NULL,
    type TEXT NOT NULL,
    channel TEXT CHECK (channel IN ('email', 'sms', 'push', 'in_app')) NOT NULL,
    subject TEXT NOT NULL,
    body TEXT NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    placeholders JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);
```

## RabbitMQ Configuration

### Queues
- **email_notifications** - Email message processing
- **sms_notifications** - SMS message processing  
- **push_notifications** - Push notification processing
- **notification_dlq** - Dead letter queue for failed messages

### Exchange
- **notifications** - Topic exchange for routing messages

### Routing Keys
- **email** - Email notifications
- **sms** - SMS notifications
- **push** - Push notifications

## Environment Variables

### RabbitMQ Configuration
```bash
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_VHOST=/
```

### Email Configuration
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@auditsystem.com",
    "FromName": "Audit System",
    "TimeoutSeconds": 30
  }
}
```

### SMS Configuration
```json
{
  "SmsSettings": {
    "Provider": "twilio",
    "ApiEndpoint": "https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "FromNumber": "+1234567890",
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "TimeoutSeconds": 30,
    "EnableUnicode": false
  }
}
```

### Push Notification Configuration
```json
{
  "PushNotificationSettings": {
    "Provider": "fcm",
    "FcmEndpoint": "https://fcm.googleapis.com/fcm/send",
    "ServerKey": "your-fcm-server-key",
    "SenderId": "your-sender-id",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "AppId": "your-app-id",
    "TimeoutSeconds": 30,
    "EnableRetry": true,
    "MaxRetries": 3
  }
}
```

## API Endpoints

### Get User Notifications
```http
GET /api/notifications?includeRead=false&limit=50
Authorization: Bearer {token}
```

### Get Organization Notifications (Manager/Admin)
```http
GET /api/notifications/organisation?includeRead=false&limit=50
Authorization: Bearer {token}
```

### Get Unread Count
```http
GET /api/notifications/unread-count
Authorization: Bearer {token}
```

### Mark Notification as Read
```http
PUT /api/notifications/{notificationId}/read
Authorization: Bearer {token}
```

### Mark Multiple Notifications as Read
```http
PUT /api/notifications/mark-read
Authorization: Bearer {token}
Content-Type: application/json

["notification-id-1", "notification-id-2"]
```

### Delete Notification
```http
DELETE /api/notifications/{notificationId}
Authorization: Bearer {token}
```

### Create System Notification (Admin)
```http
POST /api/notifications/system
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "optional-user-id",
  "organisationId": "optional-org-id", 
  "title": "System Maintenance",
  "message": "System will be down for maintenance",
  "priority": "high"
}
```

### Get Notification Templates (Admin)
```http
GET /api/notifications/templates
Authorization: Bearer {token}
```

## Usage Examples

### Creating Assignment Notifications
```csharp
// In AssignmentService.cs
public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
{
    // ... existing assignment creation logic ...
    
    // Create notification for the assigned auditor
    await _notificationService.CreateAssignmentNotificationAsync(
        assignment.AssignedToId.Value,
        assignment.AssignmentId,
        assignment.StoreInfo?["name"]?.ToString() ?? "Unknown Store"
    );
    
    return assignment;
}
```

### Creating Audit Status Notifications
```csharp
// In AuditService.cs
public async Task<Audit> ApproveAuditAsync(Guid auditId, string managerNotes)
{
    var audit = await _auditRepository.GetByIdAsync(auditId);
    audit.Status = "approved";
    audit.ManagerNotes = managerNotes;
    
    await _auditRepository.SaveChangesAsync();
    
    // Create approval notification
    await _notificationService.CreateAuditApprovedNotificationAsync(
        audit.AuditorId.Value,
        audit.AuditId,
        audit.StoreInfo?["name"]?.ToString() ?? "Unknown Store"
    );
    
    return audit;
}
```

## Service Registration

Add the following services to your `Program.cs`:

```csharp
// Notification services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Queue services
builder.Services.AddScoped<IQueueService, RabbitMQQueueService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// HTTP client for external services
builder.Services.AddHttpClient();

// Configuration
builder.Services.Configure<NotificationQueueConfig>(builder.Configuration.GetSection("NotificationQueue"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("SmsSettings"));
builder.Services.Configure<PushNotificationSettings>(builder.Configuration.GetSection("PushNotificationSettings"));

// Initialize queue service
var queueService = app.Services.GetRequiredService<IQueueService>();
await queueService.InitializeAsync();
```

## Monitoring and Health Checks

### Queue Health Check
```csharp
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
```

### Notification Status Tracking
- **pending** - Notification created, waiting to be processed
- **sent** - Notification sent to provider
- **delivered** - Notification delivered to recipient
- **failed** - Notification failed to send

## Error Handling and Retry Logic

### Retry Configuration
- **Max Retries**: 3 attempts
- **Retry Delay**: 5 seconds between attempts
- **Dead Letter Queue**: Failed messages after max retries

### Error Logging
All notification failures are logged with:
- Error message
- Retry count
- Message ID
- Channel type
- Timestamp

## Security Considerations

1. **Authentication**: All notification endpoints require valid JWT tokens
2. **Authorization**: Organization-based data isolation
3. **Input Validation**: All notification data is validated
4. **Rate Limiting**: Consider implementing rate limiting for notification creation
5. **Sensitive Data**: Email/SMS credentials should be stored securely

## Performance Optimization

1. **Database Indexes**: Optimized indexes for common queries
2. **Bulk Operations**: Support for bulk notification creation
3. **Async Processing**: All operations are asynchronous
4. **Connection Pooling**: RabbitMQ connection reuse
5. **Message Batching**: Support for batch message processing

## Testing

### Unit Tests
- Notification service business logic
- Repository data access
- Message serialization/deserialization

### Integration Tests
- RabbitMQ message processing
- Email/SMS provider integration
- Database operations

### Load Tests
- High-volume notification processing
- Queue performance under load
- Database performance with large notification volumes

## Deployment

### Docker Configuration
Add RabbitMQ to your docker-compose.yml:

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  rabbitmq_data:
```

### Database Migration
Run the notification tables SQL script:
```sql
-- Execute notification-tables.sql
```

## Troubleshooting

### Common Issues

1. **RabbitMQ Connection Failed**
   - Check RabbitMQ service is running
   - Verify connection credentials
   - Check network connectivity

2. **Email Not Sending**
   - Verify SMTP settings
   - Check email provider credentials
   - Review firewall settings

3. **SMS Not Sending**
   - Verify SMS provider API credentials
   - Check API endpoint accessibility
   - Review rate limits

4. **Push Notifications Not Working**
   - Verify FCM server key
   - Check device token validity
   - Review app configuration

### Log Analysis
Monitor logs for:
- Queue connection issues
- Message processing errors
- Provider API failures
- Database connection problems

## Future Enhancements

1. **Real-time Notifications**: WebSocket support for instant notifications
2. **Notification Preferences**: User-configurable notification settings
3. **Advanced Templates**: Dynamic template rendering with complex placeholders
4. **Analytics**: Notification delivery and engagement metrics
5. **Multi-language Support**: Internationalized notification templates
6. **Scheduled Notifications**: Future-dated notification delivery
7. **Notification Groups**: Bulk notification management
8. **Advanced Retry Strategies**: Exponential backoff, circuit breakers 