-- =====================================================
-- FIX PaymentMethod COLUMN IN PAYMENTS TABLE
-- =====================================================

BEGIN;

-- Đặt PaymentMethod nullable (vì entity không dùng field này)
ALTER TABLE "Payments" 
ALTER COLUMN "PaymentMethod" DROP NOT NULL;

-- Hoặc xóa cột này nếu không dùng
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PaymentMethod";

COMMIT;

-- Verify
\echo ''
\echo '========================================'
\echo 'PAYMENT METHOD COLUMN FIXED'
\echo '========================================'
\echo ''

SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'Payments' AND column_name = 'PaymentMethod';
