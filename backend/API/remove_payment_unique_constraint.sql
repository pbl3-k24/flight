-- Remove unique constraint on Payments.BookingId to allow multiple payment attempts per booking

-- Drop the unique index
DROP INDEX IF EXISTS "IX_Payments_BookingId";

-- Create non-unique index for performance
CREATE INDEX IF NOT EXISTS "IX_Payments_BookingId" ON "Payments" ("BookingId");

-- Verify
SELECT 
    indexname, 
    indexdef 
FROM 
    pg_indexes 
WHERE 
    tablename = 'Payments' 
    AND indexname = 'IX_Payments_BookingId';
