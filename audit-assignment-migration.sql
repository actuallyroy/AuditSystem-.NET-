-- Migration script to make assignment_id mandatory in audit table
-- Run this script to update existing audit tables

-- First, add the assignment_id column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'audit' AND column_name = 'assignment_id'
    ) THEN
        ALTER TABLE audit ADD COLUMN assignment_id UUID;
    END IF;
END $$;

-- Create a temporary assignment for existing audits that don't have an assignment_id
-- This is a fallback to ensure data integrity
INSERT INTO assignment (assignment_id, template_id, assigned_to, assigned_by, organisation_id, store_info, due_date, priority, notes, status, created_at)
SELECT 
    uuid_generate_v4(),
    a.template_id,
    a.auditor_id,
    a.auditor_id, -- Using auditor as assigned_by for existing audits
    a.organisation_id,
    a.store_info,
    a.start_time + INTERVAL '7 days', -- Default due date 7 days from start
    'medium',
    'Auto-generated assignment for existing audit',
    'fulfilled',
    a.created_at
FROM audit a
WHERE a.assignment_id IS NULL
ON CONFLICT DO NOTHING;

-- Update existing audits to link them to the auto-generated assignments
UPDATE audit 
SET assignment_id = (
    SELECT assignment_id 
    FROM assignment 
    WHERE template_id = audit.template_id 
    AND assigned_to = audit.auditor_id 
    AND organisation_id = audit.organisation_id
    AND notes = 'Auto-generated assignment for existing audit'
    LIMIT 1
)
WHERE assignment_id IS NULL;

-- Now make the column NOT NULL
ALTER TABLE audit ALTER COLUMN assignment_id SET NOT NULL;

-- Add foreign key constraint if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'audit_assignment_id_fkey'
    ) THEN
        ALTER TABLE audit 
        ADD CONSTRAINT audit_assignment_id_fkey 
        FOREIGN KEY (assignment_id) REFERENCES assignment(assignment_id) ON DELETE CASCADE;
    END IF;
END $$;

-- Create index for better performance
CREATE INDEX IF NOT EXISTS idx_audit_assignment_id ON audit(assignment_id);

-- Verify the migration
SELECT 
    'Migration completed successfully' as status,
    COUNT(*) as total_audits,
    COUNT(assignment_id) as audits_with_assignment
FROM audit; 