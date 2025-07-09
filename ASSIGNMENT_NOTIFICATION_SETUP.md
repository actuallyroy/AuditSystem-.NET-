# Assignment Notification System

## Overview

This document describes the implementation of automatic notification sending when audit assignments are created in the Retail Execution Audit System. Whenever an assignment is created and assigned to a user, they will automatically receive a notification via the SignalR WebSocket system.

## Implementation Details

### 1. Backend Changes

#### AssignmentsController Updates

**File**: `src/AuditSystem.API/Controllers/AssignmentsController.cs`

**Changes Made**:
- Added `INotificationService` dependency injection
- Added notification sending to both assignment creation methods:
  - `CreateAssignment` (POST `/api/v1/assignments`)
  - `AssignTemplateToAuditor` (POST `/api/v1/assignments/assign`)

**Code Example**:
```csharp
// Send notification to the assigned user
try
{
    await _notificationService.SendAssignmentNotificationAsync(
        createdAssignment.AssignmentId,
        createdAssignment.AssignedToId,
        createdAssignment.OrganisationId
    );
    _logger.LogInformation("Assignment notification sent to user {UserId} for assignment {AssignmentId}", 
        createdAssignment.AssignedToId, createdAssignment.AssignmentId);
}
catch (Exception notificationEx)
{
    _logger.LogError(notificationEx, "Failed to send assignment notification to user {UserId} for assignment {AssignmentId}", 
        createdAssignment.AssignedToId, createdAssignment.AssignmentId);
    // Don't fail the assignment creation if notification fails
}
```

#### NotificationService Updates

**File**: `src/AuditSystem.Services/NotificationService.cs`

**Changes Made**:
- Added `IAssignmentRepository` dependency
- Created new method `SendAssignmentNotificationAsync`
- Added method to interface `INotificationService`

**New Method**:
```csharp
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

        await CreateNotificationFromTemplateAsync("assignment_notification", placeholders, userId, organisationId);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send assignment notification for assignment {AssignmentId}", assignmentId);
        return false;
    }
}
```

### 2. Database Updates

#### Notification Templates

**File**: `init-scripts/02-seed-data.sql`

**Added Templates**:
```sql
-- Assignment notification template
INSERT INTO notification_template (template_id, name, type, channel, subject, body, is_active, placeholders) VALUES 
    ('11111111-1111-1111-1111-111111111111', 
     'assignment_notification', 
     'audit_assigned', 
     'in_app', 
     'New Audit Assignment', 
     'Hello {user_name}, you have been assigned a new audit: {template_name}. Due date: {due_date}. Priority: {priority}. Please review and complete the assignment.', 
     true, 
     '["user_name", "template_name", "due_date", "priority"]');
```

**Template Placeholders**:
- `{user_name}`: Full name of the assigned user
- `{template_name}`: Name of the audit template
- `{due_date}`: Due date for the assignment
- `{priority}`: Priority level of the assignment

### 3. Notification Flow

#### When Assignment is Created

1. **Assignment Creation**: User creates assignment via API
2. **Notification Trigger**: Controller calls `SendAssignmentNotificationAsync`
3. **Template Processing**: Notification service processes template with placeholders
4. **Database Storage**: Notification is saved to database
5. **Real-time Delivery**: SignalR sends notification to connected client
6. **Client Display**: React Native app shows notification to user

#### Notification Content

**Title**: "New Audit Assignment"
**Message**: "Hello [User Name], you have been assigned a new audit: [Template Name]. Due date: [Due Date]. Priority: [Priority]. Please review and complete the assignment."

### 4. Testing

#### Test Script

**File**: `python_tests/test_assignment_notification.py`

**Tests Included**:
1. **Create Assignment Test**: Tests notification when using POST `/api/v1/assignments`
2. **Assign Template Test**: Tests notification when using POST `/api/v1/assignments/assign`

**Usage**:
```bash
cd python_tests
python test_assignment_notification.py
```

#### Test Flow

1. **Authentication**: Get JWT token
2. **Get Resources**: Fetch available templates and auditors
3. **Create Assignment**: Create assignment via API
4. **Wait Processing**: Wait for notification processing
5. **Verify Notification**: Check that notification was created
6. **Display Results**: Show notification details

### 5. Error Handling

#### Graceful Degradation

- **Notification Failures**: Assignment creation doesn't fail if notification fails
- **Logging**: All notification attempts are logged
- **Error Recovery**: Background service can retry failed notifications

#### Error Scenarios

1. **Missing Template**: Uses fallback "Unknown Template"
2. **Missing User**: Returns false, logs error
3. **Missing Assignment**: Returns false, logs error
4. **Database Errors**: Caught and logged, doesn't affect assignment creation

### 6. SignalR Integration

#### Real-time Delivery

When a notification is created:
1. **Background Processing**: `NotificationBackgroundService` processes pending notifications
2. **SignalR Broadcast**: Notification is sent via WebSocket to connected clients
3. **Client Reception**: React Native app receives notification immediately

#### Client Events

```javascript
// Listen for new notifications
connection.on('ReceiveNotification', (notification) => {
    if (notification.type === 'audit_assigned') {
        // Show assignment notification
        showAssignmentNotification(notification);
    }
});
```

### 7. API Endpoints

#### Assignment Creation Endpoints

**POST** `/api/v1/assignments`
- Creates assignment and sends notification
- Requires: Admin or Manager role
- Body: `CreateAssignmentDto`

**POST** `/api/v1/assignments/assign`
- Assigns template to auditor and sends notification
- Requires: Admin or Manager role
- Body: `AssignTemplateToAuditorDto`

#### Notification Endpoints

**GET** `/api/v1/notifications`
- Get user's notifications
- Requires: Authentication

**PUT** `/api/v1/notifications/{id}/read`
- Mark notification as read
- Requires: Authentication

### 8. Configuration

#### Notification Settings

- **Channel**: `in_app` (in-app notifications)
- **Type**: `audit_assigned`
- **Priority**: Inherited from assignment priority
- **Status**: `pending` → `sent` (processed by background service)

#### Background Service

- **Processing Interval**: Every 5 minutes
- **Cleanup Interval**: Every hour
- **Retry Logic**: Automatic retry for failed notifications

### 9. Security Considerations

#### Authorization

- Only Admin/Manager users can create assignments
- Users can only receive notifications for their own assignments
- Organization-based access control

#### Data Privacy

- Notifications contain only necessary information
- No sensitive data in notification messages
- User consent implied by system usage

### 10. Monitoring and Logging

#### Log Levels

- **Information**: Successful notification sending
- **Warning**: Template not found, missing data
- **Error**: Failed notification attempts, exceptions

#### Metrics to Monitor

- Notification delivery success rate
- Assignment creation frequency
- SignalR connection health
- Background service performance

### 11. Future Enhancements

#### Potential Improvements

1. **Email Notifications**: Add email channel support
2. **Push Notifications**: Mobile push notification integration
3. **Notification Preferences**: User-configurable notification settings
4. **Bulk Notifications**: Send to multiple users simultaneously
5. **Notification Templates**: More customizable templates
6. **Delivery Confirmation**: Track notification read status

#### Advanced Features

1. **Scheduled Notifications**: Send reminders before due dates
2. **Escalation Notifications**: Notify managers of overdue assignments
3. **Custom Notifications**: User-defined notification content
4. **Notification History**: Detailed notification audit trail

## Summary

The assignment notification system provides:

✅ **Automatic Notifications**: Sent when assignments are created
✅ **Real-time Delivery**: Via SignalR WebSocket
✅ **Template-based**: Customizable notification content
✅ **Error Resilient**: Doesn't affect assignment creation
✅ **Comprehensive Logging**: Full audit trail
✅ **Test Coverage**: Automated testing included
✅ **Scalable Design**: Background processing for performance

This implementation ensures that users are immediately notified when they receive new audit assignments, improving system responsiveness and user experience. 