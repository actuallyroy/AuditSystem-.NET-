-- Update notification template to use in_app channel for WebSocket notifications
UPDATE notification_template 
SET channel = 'in_app', 
    updated_at = NOW()
WHERE name = 'assignment_notification';

-- Verify the update
SELECT template_id, name, type, channel, subject, body, is_active 
FROM notification_template 
WHERE name = 'assignment_notification'; 