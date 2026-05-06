-- =====================================================
-- FIX: Remove unique constraint on Payments.BookingId
-- Allow multiple payment attempts per booking
-- =====================================================

BEGIN;

-- Step 1: Drop the unique index if it exists
DROP INDEX IF EXISTS "IX_Payments_BookingId";

-- Step 2: Create a non-unique index for performance
CREATE INDEX IF NOT EXISTS "IX_Payments_BookingId" ON "Payments" ("BookingId");

-- Step 3: Verify the change
DO $$
DECLARE
    index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE tablename = 'Payments'
    AND indexname = 'IX_Payments_BookingId';
    
    IF index_count > 0 THEN
        RAISE NOTICE 'SUCCESS: Index IX_Payments_BookingId exists (non-unique)';
    ELSE
        RAISE EXCEPTION 'ERROR: Index IX_Payments_BookingId was not created';
    END IF;
END $$;

COMMIT;

-- Display current indexes on Payments table
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'Payments'
ORDER BY indexname;
