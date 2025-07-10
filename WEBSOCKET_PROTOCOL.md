# WebSocket Protocol Documentation

## Overview

The Audit System uses SignalR for real-time WebSocket communication with RabbitMQ for message queuing. This document outlines the complete message protocol for client-server communication.

## Connection Setup

### SignalR Hub URL
```
ws://your-api-domain:8080/hubs/notifications?access_token=YOUR_JWT_TOKEN
```

### Client Connection Example
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", { 
        accessTokenFactory: () => "your-jwt-token" 
    })
    .withAutomaticReconnect()
    .build();

await connection.start();
```

## Client-to-Server Messages (Methods)

### 1. Subscribe to User Notifications
```javascript
await connection.invoke("SubscribeToUser", "your-user-id");
```
**Purpose**: Subscribe to personal notifications for the authenticated user
**Parameters**: 
- `userId` (string): The user ID to subscribe to (must match authenticated user)

### 2. Join Organization Group
```javascript
await connection.invoke("JoinOrganisation", "organization-id");
```
**Purpose**: Join organization-wide notifications
**Parameters**:
- `organisationId` (string): The organization ID to join

### 3. Leave Organization Group
```javascript
await connection.invoke("LeaveOrganisation", "organization-id");
```
**Purpose**: Leave organization group
**Parameters**:
- `organisationId` (string): The organization ID to leave

### 4. Mark Single Notification as Read
```javascript
await connection.invoke("MarkNotificationAsRead", "notification-guid");
```
**Purpose**: Mark a specific notification as read
**Parameters**:
- `notificationId` (string): The notification GUID to mark as read

### 5. Mark All Notifications as Read
```javascript
await connection.invoke("MarkAllNotificationsAsRead");
```
**Purpose**: Mark all notifications as read for the current user
**Parameters**: None

### 6. Acknowledge Notification Delivery
```javascript
await connection.invoke("AcknowledgeDelivery", "notification-guid");
```
**Purpose**: Acknowledge receipt of notification delivery to ensure reliable delivery
**Parameters**:
- `notificationId` (string): The notification GUID to acknowledge

### 7. Send Test Message
```javascript
await connection.invoke("SendTestMessage", "Your test message here");
```
**Purpose**: Send a test message for debugging
**Parameters**:
- `message` (string): The test message content

## Server-to-Client Messages (Events)

### 1. Receive Notification
```javascript
connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);
    // Handle incoming notification
});
```
**Payload Structure**:
```json
{
    "notificationId": "guid",
    "type": "audit_assigned|audit_completed|system_alert|etc",
    "title": "Notification Title",
    "message": "Notification message content",
    "priority": "low|medium|high|urgent",
    "timestamp": "2024-01-01T00:00:00Z",
    "userId": "guid",
    "organisationId": "guid"
}
```

### 2. Unread Count Update
```javascript
connection.on("UnreadCount", (count) => {
    console.log("Unread notifications:", count);
    // Update UI badge
});
```
**Payload**: `number` - The current unread notification count

### 3. Notification Marked as Read Confirmation
```javascript
connection.on("NotificationMarkedAsRead", (data) => {
    console.log("Notification marked as read:", data);
    // Update UI
});
```
**Payload Structure**:
```json
{
    "notificationId": "guid",
    "unreadCount": 5
}
```

### 4. All Notifications Marked as Read Confirmation
```javascript
connection.on("AllNotificationsMarkedAsRead", (data) => {
    console.log("All notifications marked as read:", data);
    // Update UI
});
```
**Payload Structure**:
```json
{
    "unreadCount": 0
}
```

### 5. Delivery Acknowledgment Confirmation
```javascript
connection.on("DeliveryAcknowledged", (data) => {
    console.log("Delivery acknowledged:", data);
    // Update UI to show delivery confirmed
});
```
**Payload Structure**:
```json
{
    "notificationId": "guid",
    "acknowledgedAt": "2024-01-01T00:00:00Z"
}
```

### 6. Heartbeat
```javascript
connection.on("Heartbeat", (data) => {
    console.log("Connection alive:", data.timestamp);
    // Optional: Update connection status
});
```
**Payload Structure**:
```json
{
    "timestamp": "2024-01-01T00:00:00Z"
}
```

## Complete Client Implementation

```javascript
class NotificationClient {
    constructor(apiUrl, token) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiUrl}/hubs/notifications`, { 
                accessTokenFactory: () => token 
            })
            .withAutomaticReconnect()
            .build();
        
        this.setupEventHandlers();
    }

    setupEventHandlers() {
        // Handle new notifications
        this.connection.on("ReceiveNotification", (notification) => {
            this.handleNewNotification(notification);
            // Automatically acknowledge delivery
            this.acknowledgeDelivery(notification.notificationId);
        });

        // Handle unread count updates
        this.connection.on("UnreadCount", (count) => {
            this.updateUnreadBadge(count);
        });

        // Handle read confirmations
        this.connection.on("NotificationMarkedAsRead", (data) => {
            this.handleNotificationRead(data);
        });

        this.connection.on("AllNotificationsMarkedAsRead", (data) => {
            this.handleAllNotificationsRead(data);
        });

        // Handle delivery acknowledgment confirmation
        this.connection.on("DeliveryAcknowledged", (data) => {
            this.handleDeliveryAcknowledged(data);
        });

        // Handle heartbeat
        this.connection.on("Heartbeat", (data) => {
            this.updateConnectionStatus(true);
        });

        // Handle connection events
        this.connection.onreconnecting(() => {
            this.updateConnectionStatus(false);
        });

        this.connection.onreconnected(() => {
            this.updateConnectionStatus(true);
        });
    }

    async connect(userId, organisationId) {
        try {
            await this.connection.start();
            
            // Subscribe to notifications
            await this.connection.invoke("SubscribeToUser", userId);
            if (organisationId) {
                await this.connection.invoke("JoinOrganisation", organisationId);
            }
            
            console.log("Connected to notification hub");
        } catch (error) {
            console.error("Failed to connect:", error);
        }
    }

    async markAsRead(notificationId) {
        try {
            await this.connection.invoke("MarkNotificationAsRead", notificationId);
        } catch (error) {
            console.error("Failed to mark notification as read:", error);
        }
    }

    async markAllAsRead() {
        try {
            await this.connection.invoke("MarkAllNotificationsAsRead");
        } catch (error) {
            console.error("Failed to mark all notifications as read:", error);
        }
    }

    async acknowledgeDelivery(notificationId) {
        try {
            await this.connection.invoke("AcknowledgeDelivery", notificationId);
        } catch (error) {
            console.error("Failed to acknowledge delivery:", error);
        }
    }

    async sendTestMessage(message) {
        try {
            await this.connection.invoke("SendTestMessage", message);
        } catch (error) {
            console.error("Failed to send test message:", error);
        }
    }

    // UI update methods (implement as needed)
    handleNewNotification(notification) {
        // Show notification toast, update list, etc.
        console.log("New notification:", notification);
    }

    updateUnreadBadge(count) {
        // Update unread badge in UI
        console.log("Unread count:", count);
    }

    handleNotificationRead(data) {
        // Update UI when notification is marked as read
        console.log("Notification read:", data);
    }

    handleAllNotificationsRead(data) {
        // Update UI when all notifications are marked as read
        console.log("All notifications read:", data);
    }

    handleDeliveryAcknowledged(data) {
        // Update UI when delivery is acknowledged
        console.log("Delivery acknowledged:", data);
        // Could update notification status in UI to show "delivered"
    }

    updateConnectionStatus(connected) {
        // Update connection status in UI
        console.log("Connection status:", connected ? "Connected" : "Disconnected");
    }

    disconnect() {
        this.connection.stop();
    }
}

// Usage example
const notificationClient = new NotificationClient(
    "http://localhost:8080", 
    "your-jwt-token"
);

await notificationClient.connect("user-id", "org-id");
```

## RabbitMQ Integration

The system uses RabbitMQ for reliable message queuing:

### Queue Configuration
- **Exchange**: `notification_exchange`
- **Queue**: `notification_queue`
- **Routing Key**: `notification`

### Message Format
```json
{
    "userId": "guid",
    "organisationId": "guid",
    "type": "notification_type",
    "title": "Notification Title",
    "message": "Notification message",
    "priority": "medium"
}
```

### Benefits
1. **Reliability**: Messages are persisted and survive service restarts
2. **Scalability**: Multiple instances can process notifications
3. **Performance**: Asynchronous processing reduces API response times
4. **Fault Tolerance**: Failed messages are automatically retried

## Error Handling

### Connection Errors
```javascript
this.connection.onclose((error) => {
    console.error("Connection closed:", error);
    // Implement reconnection logic
});
```

### Method Invocation Errors
```javascript
try {
    await this.connection.invoke("MarkNotificationAsRead", notificationId);
} catch (error) {
    console.error("Failed to mark notification as read:", error);
    // Handle error (show user message, retry, etc.)
}
```

## Security

### Authentication
- All connections require valid JWT token
- Token is passed via `access_token` query parameter
- Server validates token and extracts user claims

### Authorization
- Users can only subscribe to their own notifications
- Users can only join their organization's group
- Users can only mark their own notifications as read

## Performance Considerations

1. **Connection Limits**: SignalR supports up to 100,000 concurrent connections
2. **Message Size**: Keep notification messages under 1MB
3. **Rate Limiting**: API has rate limiting (100 requests per minute)
4. **Reconnection**: Automatic reconnection with exponential backoff

## Troubleshooting

### Common Issues

1. **Connection Fails**: Check JWT token validity and network connectivity
2. **Messages Not Received**: Verify user is subscribed to correct groups
3. **Unread Count Not Updated**: Ensure proper error handling in mark-as-read methods
4. **High Memory Usage**: Monitor connection count and implement proper cleanup

### Debug Mode
Enable detailed SignalR errors in development:
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
``` 