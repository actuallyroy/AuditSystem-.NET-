-- Seed initial data for development and testing
-- This script will run after the main database schema is created

-- Insert test organisations
INSERT INTO organisation (organisation_id, name, region, type) VALUES 
    ('f47ac10b-58cc-4372-a567-0e02b2c3d479', 'ACME Retail Corp', 'North America', 'Retail Chain'),
    ('6ba7b810-9dad-11d1-80b4-00c04fd430c8', 'Fresh Foods Inc', 'Europe', 'Grocery Chain'),
    ('6ba7b811-9dad-11d1-80b4-00c04fd430c8', 'Quick Mart Chain', 'Asia Pacific', 'Convenience Store');

-- Insert test users
INSERT INTO users (user_id, username, email, first_name, last_name, password_hash, role, organisation_id, is_active) VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'admin', 'admin@acme-retail.com', 'System', 'Administrator', '$2a$11$8gE7VOF7b9N9qzQvK8K8OuZ8xFJhQpQhGqI2F1K5F2K3K4K5K6K7K8', 'admin', 'f47ac10b-58cc-4372-a567-0e02b2c3d479', true),
    ('550e8400-e29b-41d4-a716-446655440001', 'manager1', 'manager@acme-retail.com', 'John', 'Manager', '$2a$11$8gE7VOF7b9N9qzQvK8K8OuZ8xFJhQpQhGqI2F1K5F2K3K4K5K6K7K8', 'manager', 'f47ac10b-58cc-4372-a567-0e02b2c3d479', true),
    ('550e8400-e29b-41d4-a716-446655440002', 'supervisor1', 'supervisor@acme-retail.com', 'Jane', 'Supervisor', '$2a$11$8gE7VOF7b9N9qzQvK8K8OuZ8xFJhQpQhGqI2F1K5F2K3K4K5K6K7K8', 'supervisor', 'f47ac10b-58cc-4372-a567-0e02b2c3d479', true),
    ('550e8400-e29b-41d4-a716-446655440003', 'auditor1', 'auditor1@acme-retail.com', 'Mike', 'Auditor', '$2a$11$8gE7VOF7b9N9qzQvK8K8OuZ8xFJhQpQhGqI2F1K5F2K3K4K5K6K7K8', 'auditor', 'f47ac10b-58cc-4372-a567-0e02b2c3d479', true),
    ('550e8400-e29b-41d4-a716-446655440004', 'auditor2', 'auditor2@acme-retail.com', 'Sarah', 'Field Agent', '$2a$11$8gE7VOF7b9N9qzQvK8K8OuZ8xFJhQpQhGqI2F1K5F2K3K4K5K6K7K8', 'auditor', 'f47ac10b-58cc-4372-a567-0e02b2c3d479', true);

-- Insert sample template
INSERT INTO template (template_id, name, description, category, questions, scoring_rules, created_by, is_published) VALUES 
     ('123e4567-e89b-12d3-a456-426614174000', 
     'Store Compliance Audit', 
     'Basic store compliance and merchandising audit template',
     'Compliance',
     '[
        {
            "id": "section_1",
            "title": "Store Appearance",
            "description": "Evaluate overall store appearance and cleanliness",
            "order": 1,
            "isRequired": true,
            "questions": [
                {
                    "id": "q1_1",
                    "type": "SingleChoice",
                    "title": "Is the store entrance clean and welcoming?",
                    "options": ["Yes", "No", "Partially"],
                    "required": true,
                    "scoring": { "Yes": 10, "Partially": 5, "No": 0 }
                },
                {
                    "id": "q1_2",
                    "type": "Text",
                    "title": "Additional comments on store appearance",
                    "required": false
                }
            ]
        },
        {
            "id": "section_2",
            "title": "Product Display",
            "description": "Check product merchandising and display quality",
            "order": 2,
            "isRequired": true,
            "questions": [
                {
                    "id": "q2_1",
                    "type": "MultipleChoice",
                    "title": "Which areas need attention?",
                    "options": ["Shelf organization", "Product labeling", "Promotional displays", "Stock levels"],
                    "required": false
                },
                {
                    "id": "q2_2",
                    "type": "FileUpload",
                    "title": "Upload photo of main display area",
                    "required": true
                }
            ]
        }
     ]',
     '{"enabled": true, "method": "weighted", "passThreshold": 70}',
     '550e8400-e29b-41d4-a716-446655440001',
     true);

-- Insert sample audit assignment
INSERT INTO assignment (assignment_id, template_id, assigned_to, assigned_by, organisation_id, store_info, due_date, status, priority) VALUES 
    ('987fcdeb-51a2-4567-8901-234567890123',
     '123e4567-e89b-12d3-a456-426614174000',
     '550e8400-e29b-41d4-a716-446655440003',
     '550e8400-e29b-41d4-a716-446655440001',
     'f47ac10b-58cc-4372-a567-0e02b2c3d479',
     '{"store_name": "ACME Downtown Store", "address": "123 Main Street, Downtown, City, State 12345"}',
     CURRENT_DATE + INTERVAL '7 days',
     'pending',
     'high');

-- Insert sample completed audit
INSERT INTO audit (audit_id, template_id, auditor_id, organisation_id, status, score, responses, media, location, store_info) VALUES 
    ('456789ab-cdef-1234-5678-9abcdef01234',
     '123e4567-e89b-12d3-a456-426614174000',
     '550e8400-e29b-41d4-a716-446655440003',
     'f47ac10b-58cc-4372-a567-0e02b2c3d479',
     'submitted',
     85.5,
     '{
        "section_1": {
            "q1_1": "Yes",
            "q1_2": "Store entrance looks great, recently renovated"
        },
        "section_2": {
            "q2_1": ["Stock levels"],
            "q2_2": "photo_12345.jpg"
        }
     }',
     '["photo_12345.jpg"]',
     '{"latitude": 40.7128, "longitude": -74.0060, "accuracy": 5.0}',
     '{"store_name": "ACME Downtown Store", "address": "123 Main Street"}');

-- Insert notification templates
INSERT INTO notification_template (template_id, name, type, channel, subject, body, is_active, placeholders) VALUES 
    ('11111111-1111-1111-1111-111111111111', 
     'assignment_notification', 
     'audit_assigned', 
     'in_app', 
     'New Audit Assignment', 
     'Hello {user_name}, you have been assigned a new audit: {template_name}. Due date: {due_date}. Priority: {priority}. Please review and complete the assignment.', 
     true, 
     '["user_name", "template_name", "due_date", "priority"]'),
    
    ('22222222-2222-2222-2222-222222222222', 
     'audit_completed_notification', 
     'audit_completed', 
     'in_app', 
     'Audit Completed', 
     'Hello {user_name}, the audit for {store_name} has been completed successfully. Thank you for your work!', 
     true, 
     '["user_name", "store_name"]'),
    
    ('33333333-3333-3333-3333-333333333333', 
     'audit_approved_notification', 
     'audit_reviewed', 
     'in_app', 
     'Audit Approved', 
     'Hello {user_name}, your audit for {store_name} has been approved with a score of {score}. Critical issues found: {critical_issues}.', 
     true, 
     '["user_name", "store_name", "score", "critical_issues"]'),
    
    ('44444444-4444-4444-4444-444444444444', 
     'audit_rejected_notification', 
     'audit_reviewed', 
     'in_app', 
     'Audit Requires Revision', 
     'Hello {user_name}, your audit for {store_name} requires revision. Reason: {reason}. Please review and resubmit.', 
     true, 
     '["user_name", "store_name", "reason"]'),
    
    ('55555555-5555-5555-5555-555555555555', 
     'system_notification', 
     'system_alert', 
     'in_app', 
     'System Alert', 
     'System notification: {message}', 
     true, 
     '["message"]');

-- Insert system logs for demonstration
INSERT INTO log (log_id, entity_type, entity_id, action, user_id, metadata) VALUES 
    ('789abcde-f012-3456-7890-abcdef012345', 'Template', '123e4567-e89b-12d3-a456-426614174000', 'Created', '550e8400-e29b-41d4-a716-446655440001', '{"message": "Template created successfully"}'),
    ('89abcdef-0123-4567-890a-bcdef0123456', 'Assignment', '987fcdeb-51a2-4567-8901-234567890123', 'Created', '550e8400-e29b-41d4-a716-446655440001', '{"message": "Assignment created for store audit"}'),
    ('9abcdef0-1234-5678-90ab-cdef01234567', 'Audit', '456789ab-cdef-1234-5678-9abcdef01234', 'Submitted', '550e8400-e29b-41d4-a716-446655440003', '{"message": "Audit submitted with score 85.5"}');

-- Create indexes for better performance on seed data
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_template_category ON template(category);
CREATE INDEX IF NOT EXISTS idx_assignment_status ON assignment(status);
CREATE INDEX IF NOT EXISTS idx_audit_status ON audit(status);
CREATE INDEX IF NOT EXISTS idx_log_logged_at ON log(logged_at);

-- Print completion message
DO $$
BEGIN
    RAISE NOTICE 'Seed data inserted successfully!';
    RAISE NOTICE 'Test users created:';
    RAISE NOTICE '  - admin@acme-retail.com (admin)';
    RAISE NOTICE '  - manager@acme-retail.com (manager)';  
    RAISE NOTICE '  - supervisor@acme-retail.com (supervisor)';
    RAISE NOTICE '  - auditor1@acme-retail.com (auditor)';
    RAISE NOTICE '  - auditor2@acme-retail.com (auditor)';
    RAISE NOTICE 'Default password hash: $2a$11$8gE7VOF7b9N9qzQvK8K8Ou... (use BCrypt to verify)';
    RAISE NOTICE 'Sample template and audit data created for testing.';
END $$; 