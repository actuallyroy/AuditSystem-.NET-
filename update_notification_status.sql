-- Migration: Add "broadcasted" status to notification table
-- This allows notifications to be marked as broadcasted (sent via SignalR) 
-- but not yet delivered (waiting for client acknowledgment)

-- Drop the existing constraint
ALTER TABLE notification DROP CONSTRAINT IF EXISTS notification_status_check;

-- Add the new constraint with "broadcasted" status
ALTER TABLE notification ADD CONSTRAINT notification_status_check 
    CHECK (status IN ('pending', 'sent', 'broadcasted', 'failed', 'delivered'));

-- Update any existing "sent" notifications that should be "broadcasted"
-- (This is optional - only if you want to migrate existing data)
-- UPDATE notification SET status = 'broadcasted' WHERE status = 'sent' AND delivered_at IS NULL;

-- Log the migration
DO $$
BEGIN
    RAISE NOTICE 'Migration completed: Added "broadcasted" status to notification table';
    RAISE NOTICE 'New status flow: pending -> sent -> broadcasted -> delivered';
    RAISE NOTICE 'Or: pending -> sent -> broadcasted -> failed (if acknowledgment fails)';
END $$; 