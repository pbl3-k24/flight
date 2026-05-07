-- =====================================================
-- FIX PAYMENTS TABLE SCHEMA
-- =====================================================

BEGIN;

-- Thêm các cột audit nếu chưa có
ALTER TABLE "Payments" 
ADD COLUMN IF NOT EXISTS "CreatedBy" INTEGER,
ADD COLUMN IF NOT EXISTS "UpdatedBy" INTEGER,
ADD COLUMN IF NOT EXISTS "Currency" VARCHAR(10) DEFAULT 'VND';

-- Thêm các cột khác nếu thiếu
ALTER TABLE "Payments" 
ADD COLUMN IF NOT EXISTS "IsDeleted" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "DeletedAt" TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS "Version" INTEGER DEFAULT 0;

COMMIT;

-- Verify
\echo ''
\echo '========================================'
\echo 'PAYMENTS SCHEMA FIXED'
\echo '========================================'
\echo ''

SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Payments'
ORDER BY ordinal_position;
