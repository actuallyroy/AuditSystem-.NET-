-- Notification System Tables
-- Add these tables to your existing database schema

-- 8. NOTIFICATION
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

-- 9. NOTIFICATION_TEMPLATE
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

-- Create indexes for better performance
CREATE INDEX idx_notification_user_id ON notification(user_id);
CREATE INDEX idx_notification_organisation_id ON notification(organisation_id);
CREATE INDEX idx_notification_status ON notification(status);
CREATE INDEX idx_notification_created_at ON notification(created_at);
CREATE INDEX idx_notification_type ON notification(type);
CREATE INDEX idx_notification_channel ON notification(channel);
CREATE INDEX idx_notification_unread ON notification(user_id, is_read) WHERE is_read = FALSE;

-- Create indexes for notification templates
CREATE INDEX idx_notification_template_name ON notification_template(name);
CREATE INDEX idx_notification_template_type ON notification_template(type);
CREATE INDEX idx_notification_template_channel ON notification_template(channel);
CREATE INDEX idx_notification_template_active ON notification_template(is_active) WHERE is_active = TRUE;

-- Insert some default notification templates
INSERT INTO notification_template (template_id, name, type, channel, subject, body, placeholders) VALUES
(
    uuid_generate_v4(),
    'assignment_notification',
    'assignment',
    'email',
    'New Audit Assignment - {store_name}',
    'Hello {user_name},<br><br>You have been assigned a new audit for {store_name}.<br><br>Please complete this audit by {due_date}.<br><br>Best regards,<br>Audit System',
    '{"user_name": "string", "store_name": "string", "due_date": "date"}'
),
(
    uuid_generate_v4(),
    'audit_completed_notification',
    'audit_completed',
    'email',
    'Audit Completed - {store_name}',
    'Hello {user_name},<br><br>Your audit for {store_name} has been completed and submitted for review.<br><br>You will be notified once the review is complete.<br><br>Best regards,<br>Audit System',
    '{"user_name": "string", "store_name": "string"}'
),
(
    uuid_generate_v4(),
    'audit_approved_notification',
    'audit_approved',
    'email',
    'Audit Approved - {store_name}',
    'Hello {user_name},<br><br>Congratulations! Your audit for {store_name} has been approved.<br><br>Score: {score}<br>Critical Issues: {critical_issues}<br><br>Best regards,<br>Audit System',
    '{"user_name": "string", "store_name": "string", "score": "number", "critical_issues": "number"}'
),
(
    uuid_generate_v4(),
    'audit_rejected_notification',
    'audit_rejected',
    'email',
    'Audit Rejected - {store_name}',
    'Hello {user_name},<br><br>Your audit for {store_name} has been rejected.<br><br>Reason: {reason}<br><br>Please review and resubmit the audit.<br><br>Best regards,<br>Audit System',
    '{"user_name": "string", "store_name": "string", "reason": "string"}'
),
(
    uuid_generate_v4(),
    'system_notification',
    'system',
    'in_app',
    'System Notification',
    '{message}',
    '{"message": "string"}'
); 