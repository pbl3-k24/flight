-- =====================================================
-- ADD MISSING FIELDS TO BookingPassengers
-- =====================================================

BEGIN;

-- Thêm các cột mới
ALTER TABLE "BookingPassengers" 
ADD COLUMN IF NOT EXISTS "FirstName" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "LastName" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "Email" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "Phone" VARCHAR(20),
ADD COLUMN IF NOT EXISTS "Nationality" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "PassportNumber" VARCHAR(50);

-- Cập nhật dữ liệu cũ (nếu có) - tách FullName thành FirstName và LastName
UPDATE "BookingPassengers"
SET 
    "FirstName" = SPLIT_PART("FullName", ' ', 1),
    "LastName" = CASE 
        WHEN POSITION(' ' IN "FullName") > 0 
        THEN SUBSTRING("FullName" FROM POSITION(' ' IN "FullName") + 1)
        ELSE ''
    END
WHERE "FirstName" IS NULL;

-- Đặt NOT NULL cho các trường bắt buộc (sau khi đã cập nhật dữ liệu)
ALTER TABLE "BookingPassengers" 
ALTER COLUMN "FirstName" SET NOT NULL,
ALTER COLUMN "LastName" SET NOT NULL,
ALTER COLUMN "Email" SET NOT NULL,
ALTER COLUMN "Phone" SET NOT NULL;

-- Tạo index cho email và phone để search nhanh
CREATE INDEX IF NOT EXISTS "IX_BookingPassengers_Email" 
ON "BookingPassengers" ("Email");

CREATE INDEX IF NOT EXISTS "IX_BookingPassengers_Phone" 
ON "BookingPassengers" ("Phone");

CREATE INDEX IF NOT EXISTS "IX_BookingPassengers_PassportNumber" 
ON "BookingPassengers" ("PassportNumber");

COMMIT;

-- Verify
\echo ''
\echo '========================================'
\echo 'BOOKING PASSENGERS SCHEMA UPDATED'
\echo '========================================'
\echo ''

SELECT 
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'BookingPassengers'
ORDER BY ordinal_position;
