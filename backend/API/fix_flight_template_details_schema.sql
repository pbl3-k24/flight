-- Fix FlightTemplateDetails schema
-- Thêm các cột còn thiếu

BEGIN;

-- Thêm CreatedAt nếu chưa có
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'CreatedAt'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        ADD COLUMN "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW();
    END IF;
END $$;

-- Thêm DayOfWeek nếu chưa có
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'DayOfWeek'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        ADD COLUMN "DayOfWeek" INTEGER NOT NULL DEFAULT 0;
    END IF;
END $$;

-- Thêm FlightNumberPrefix nếu chưa có
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'FlightNumberPrefix'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        ADD COLUMN "FlightNumberPrefix" VARCHAR(10) NOT NULL DEFAULT 'VN';
    END IF;
END $$;

-- Thêm FlightNumberSuffix nếu chưa có
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'FlightNumberSuffix'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        ADD COLUMN "FlightNumberSuffix" VARCHAR(10) NOT NULL DEFAULT '000';
    END IF;
END $$;

-- Xóa cột FlightNumber cũ nếu có (đã thay bằng Prefix + Suffix)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'FlightNumber'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        DROP COLUMN "FlightNumber";
    END IF;
END $$;

-- Xóa cột OperatingDays nếu có (không dùng nữa, dùng DayOfWeek)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'OperatingDays'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        DROP COLUMN "OperatingDays";
    END IF;
END $$;

-- Xóa cột ArrivalOffsetDays nếu có (không dùng nữa)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'FlightTemplateDetails' 
        AND column_name = 'ArrivalOffsetDays'
    ) THEN
        ALTER TABLE "FlightTemplateDetails" 
        DROP COLUMN "ArrivalOffsetDays";
    END IF;
END $$;

COMMIT;

-- Verify
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'FlightTemplateDetails'
ORDER BY ordinal_position;
