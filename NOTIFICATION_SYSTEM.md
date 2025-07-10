# Notification System Documentation

## Overview

The notification system provides real-time notifications for the Retail Execution Audit System using SignalR for WebSocket connections and a REST API for notification management. It supports both user-specific and organization-wide notifications with different priority levels and delivery channels.

## Architecture

### Components

1. **Notification Entities** (`AuditSystem.Domain.Entities`)
   - `Notification`: Core notification entity
   - `NotificationTemplate`: Reusable notification templates

2. **Data Access Layer** (`AuditSystem.Infrastructure.Repositories`)
   - `NotificationRepository`: CRUD operations for notifications
   - `NotificationTemplateRepository`: Template management

3. **Business Logic** (`AuditSystem.Services`)
   - `NotificationService`: Core notification business logic
   - `NotificationBackgroundService`: Background processing tasks

4. **API Layer** (`AuditSystem.API`)
   - `NotificationsController`: REST API endpoints
   - `NotificationHub`: SignalR real-time communication

## Database Schema

### Notification Table
```sql
CREATE TABLE notification (
    notification_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    organisation_id UUID REFERENCES organisation(organisation_id) ON DELETE CASCADE,
    type TEXT NOT NULL,
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

## API Endpoints

### REST API

#### Get User Notifications
```http
GET /api/v1/notifications?page=1&pageSize=20
Authorization: Bearer {token}
```

#### Get Unread Count
```http
GET /api/v1/notifications/unread-count
Authorization: Bearer {token}
```

#### Mark Notification as Read
```http
PUT /api/v1/notifications/{notificationId}/read
Authorization: Bearer {token}
```

#### Mark All Notifications as Read
```http
PUT /api/v1/notifications/mark-all-read
Authorization: Bearer {token}
```

#### Delete Notification
```http
DELETE /api/v1/notifications/{notificationId}
Authorization: Bearer {token}
```

#### Send System Alert (Admin/Manager only)
```http
POST /api/v1/notifications/system-alert
Authorization: Bearer {token}
Content-Type: application/json

{
    "title": "System Alert",
    "message": "Important system message",
    "priority": "high"
}
```

#### Send Bulk Notification (Admin only)
```http
POST /api/v1/notifications/bulk
Authorization: Bearer {token}
Content-Type: application/json

{
    "title": "Bulk Notification",
    "message": "Message for multiple users",
    "userIds": ["user-id-1", "user-id-2"]
}
```

#### Get Organization Notifications (Admin/Manager only)
```http
GET /api/v1/notifications/organisation?page=1&pageSize=20
Authorization: Bearer {token}
```

### SignalR Hub

#### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("ws://192.168.1.4:8080/hubs/notifications", {
        accessTokenFactory: () => token
    })
    .build();
```

#### Hub Methods

##### Subscribe to User Notifications
```javascript
await connection.invoke("SubscribeToUser", userId);
```

##### Join Organization Group
```javascript
await connection.invoke("JoinOrganisation", organisationId);
```

##### Leave Organization Group
```javascript
await connection.invoke("LeaveOrganisation", organisationId);
```

##### Send Test Message
```javascript
await connection.invoke("SendTestMessage", "Hello from client!");
```

##### Mark Notification as Read
```javascript
await connection.invoke("MarkNotificationAsRead", notificationId);
```

##### Mark All Notifications as Read
```javascript
await connection.invoke("MarkAllNotificationsAsRead");
```

#### Hub Events

##### Receive Notification
```javascript
connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);
});
```

##### Unread Count Update
```javascript
connection.on("UnreadCount", (count) => {
    console.log("Unread count:", count);
});
```

##### Heartbeat
```javascript
connection.on("Heartbeat", (data) => {
    console.log("Heartbeat received:", data.timestamp);
});
```

## Notification Types

### Built-in Types
- `audit_assigned`: When an audit is assigned to a user
- `audit_completed`: When an audit is completed
- `audit_reviewed`: When an audit is reviewed (approved/rejected)
- `system_alert`: System-wide notifications

### Custom Types
You can create custom notification types by using the generic notification creation methods.

## Notification Templates

### Default Templates

#### Assignment Notification
```json
{
    "name": "assignment_notification",
    "type": "assignment",
    "channel": "email",
    "subject": "New Audit Assignment - {store_name}",
    "body": "Hello {user_name},<br><br>You have been assigned a new audit for {store_name}.<br><br>Please complete this audit by {due_date}.<br><br>Best regards,<br>Audit System",
    "placeholders": {
        "user_name": "string",
        "store_name": "string",
        "due_date": "date"
    }
}
```

#### Audit Completed Notification
```json
{
    "name": "audit_completed_notification",
    "type": "audit_completed",
    "channel": "email",
    "subject": "Audit Completed - {store_name}",
    "body": "Hello {user_name},<br><br>Your audit for {store_name} has been completed and submitted for review.<br><br>You will be notified once the review is complete.<br><br>Best regards,<br>Audit System",
    "placeholders": {
        "user_name": "string",
        "store_name": "string"
    }
}
```

#### Audit Approved Notification
```json
{
    "name": "audit_approved_notification",
    "type": "audit_approved",
    "channel": "email",
    "subject": "Audit Approved - {store_name}",
    "body": "Hello {user_name},<br><br>Congratulations! Your audit for {store_name} has been approved.<br><br>Score: {score}<br>Critical Issues: {critical_issues}<br><br>Best regards,<br>Audit System",
    "placeholders": {
        "user_name": "string",
        "store_name": "string",
        "score": "number",
        "critical_issues": "number"
    }
}
```

#### Audit Rejected Notification
```json
{
    "name": "audit_rejected_notification",
    "type": "audit_rejected",
    "channel": "email",
    "subject": "Audit Rejected - {store_name}",
    "body": "Hello {user_name},<br><br>Your audit for {store_name} has been rejected.<br><br>Reason: {reason}<br><br>Please review and resubmit the audit.<br><br>Best regards,<br>Audit System",
    "placeholders": {
        "user_name": "string",
        "store_name": "string",
        "reason": "string"
    }
}
```

#### System Notification
```json
{
    "name": "system_notification",
    "type": "system",
    "channel": "in_app",
    "subject": "System Notification",
    "body": "{message}",
    "placeholders": {
        "message": "string"
    }
}
```

## Usage Examples

### Creating Notifications Programmatically

```csharp
// Create a simple notification
var notification = new Notification
{
    UserId = userId,
    Type = "custom_type",
    Title = "Custom Title",
    Message = "Custom message content",
    Priority = "medium",
    Channel = "in_app"
};

await _notificationService.CreateNotificationAsync(notification);

// Create notification from template
var placeholders = new Dictionary<string, object>
{
    { "user_name", "John Doe" },
    { "store_name", "Store ABC" },
    { "due_date", "2024-01-15" }
};

await _notificationService.CreateNotificationFromTemplateAsync(
    "assignment_notification", 
    placeholders, 
    userId, 
    organisationId
);
```

### Sending Audit-Related Notifications

```csharp
// Send audit assignment notification
await _notificationService.SendAuditAssignmentNotificationAsync(
    auditId, userId, organisationId
);

// Send audit completed notification
await _notificationService.SendAuditCompletedNotificationAsync(
    auditId, userId, organisationId
);

// Send audit reviewed notification
await _notificationService.SendAuditReviewedNotificationAsync(
    auditId, userId, organisationId, "approved", 85.5m
);
```

### Sending System Notifications

```csharp
// Send system alert to organization
await _notificationService.SendSystemAlertAsync(
    "System Maintenance", 
    "System will be down for maintenance", 
    organisationId, 
    "high"
);

// Send bulk notification to specific users
var userIds = new List<Guid> { user1Id, user2Id, user3Id };
await _notificationService.SendBulkNotificationAsync(
    "Important Update", 
    "Please update your profile information", 
    userIds, 
    organisationId
);
```

### Using SignalR Hub Methods

```csharp
// Send notification to specific user
await NotificationHub.SendNotificationToUser(
    hubContext, 
    userId, 
    new { type = "custom", title = "Hello", message = "World" }
);

// Send notification to organization
await NotificationHub.SendNotificationToOrganisation(
    hubContext, 
    organisationId, 
    new { type = "system", title = "System Alert", message = "Maintenance scheduled" }
);

// Send notification to all connected clients
await NotificationHub.SendNotificationToAll(
    hubContext, 
    new { type = "broadcast", title = "Broadcast", message = "Important message" }
);

// Update unread count for user
await NotificationHub.UpdateUnreadCount(hubContext, userId, 5);

// Send heartbeat
await NotificationHub.SendHeartbeat(hubContext);
```

## Configuration

### SignalR Configuration

The SignalR hub is configured with the following settings:

- **URL**: `ws://192.168.1.4:8080/hubs/notifications`
- **Authentication**: JWT token via query parameter `access_token`
- **Transport**: WebSockets only
- **Reconnection**: Automatic with exponential backoff

### Background Service Configuration

The `NotificationBackgroundService` runs with the following intervals:

- **Processing Interval**: 5 minutes (processes pending notifications)
- **Cleanup Interval**: 1 hour (removes expired notifications)

## Security

### Authentication
- All API endpoints require JWT authentication
- SignalR connections require valid JWT token
- User can only access their own notifications
- Organization notifications require admin/manager role

### Authorization
- `AdminOrManager` policy required for system alerts
- `AdminOnly` policy required for bulk notifications
- Users can only mark their own notifications as read

## Testing

### Running Tests

```bash
# Test notification system
python python_tests/test_notifications.py

# Test with specific user
python python_tests/test_notifications.py --user admin
```

### Test Coverage

The test script covers:
- User authentication
- Notification retrieval
- Unread count tracking
- Marking notifications as read
- System alert sending (with permission checks)
- SignalR connection testing

## Monitoring and Logging

### Log Levels
- **Information**: Connection events, notification creation
- **Warning**: Permission violations, failed deliveries
- **Error**: Service failures, database errors
- **Debug**: Background service operations

### Key Metrics
- Notification delivery success rate
- SignalR connection count
- Unread notification counts
- Background service performance

## Troubleshooting

### Common Issues

1. **SignalR Connection Fails**
   - Check if the hub URL is correct
   - Verify JWT token is valid and not expired
   - Ensure WebSocket transport is enabled

2. **Notifications Not Appearing**
   - Check notification service is running
   - Verify user permissions
   - Check database connectivity

3. **Background Service Not Working**
   - Check service registration in Program.cs
   - Verify Microsoft.Extensions.Hosting package is installed
   - Check application logs for errors

### Debug Commands

```bash
# Check notification tables
psql -d audit_system -c "SELECT COUNT(*) FROM notification;"

# Check active SignalR connections
psql -d audit_system -c "SELECT * FROM log WHERE action LIKE '%SignalR%';"

# Check notification templates
psql -d audit_system -c "SELECT name, type, is_active FROM notification_template;"
```

## Future Enhancements

### Planned Features
- Email notification delivery
- SMS notification support
- Push notification integration
- Notification preferences per user
- Advanced filtering and search
- Notification analytics dashboard
- Webhook support for external integrations

### Performance Optimizations
- Redis caching for frequently accessed notifications
- Database indexing optimization
- SignalR connection pooling
- Background job queuing with RabbitMQ 