-- =====================================================
-- MIGRATE FLIGHTS SCHEMA TO USE FLIGHTDEFINITION
-- =====================================================

BEGIN;

-- Bước 1: Xóa tất cả flights cũ (vì không có FlightDefinitionId)
DELETE FROM "FlightSeatInventories";
DELETE FROM "Flights";

-- Bước 2: Xóa các cột cũ không cần thiết
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "FlightNumber";
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "RouteId";
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "AircraftId";

-- Bước 3: Đặt FlightDefinitionId là NOT NULL
ALTER TABLE "Flights" ALTER COLUMN "FlightDefinitionId" SET NOT NULL;

-- Bước 4: Tạo index cho FlightDefinitionId nếu chưa có
CREATE INDEX IF NOT EXISTS "IX_Flights_FlightDefinitionId" 
ON "Flights" ("FlightDefinitionId");

-- Bước 5: Tạo index cho DepartureTime để search nhanh
CREATE INDEX IF NOT EXISTS "IX_Flights_DepartureTime" 
ON "Flights" ("DepartureTime");

-- Bước 6: Tạo index composite cho search
CREATE INDEX IF NOT EXISTS "IX_Flights_FlightDefinitionId_DepartureTime" 
ON "Flights" ("FlightDefinitionId", "DepartureTime");

COMMIT;

-- Verify
\echo ''
\echo '========================================'
\echo 'FLIGHTS SCHEMA MIGRATED'
\echo '========================================'
\echo ''

SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'Flights'
ORDER BY ordinal_position;
