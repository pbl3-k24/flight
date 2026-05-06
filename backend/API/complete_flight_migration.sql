-- =====================================================
-- Complete Flight Migration - Drop Old Columns
-- =====================================================
-- This script removes the old columns after verifying migration success

-- Step 1: Verify all flights have FlightDefinitionId
DO $$
DECLARE
    flights_without_definition INTEGER;
BEGIN
    SELECT COUNT(*) INTO flights_without_definition
    FROM "Flights"
    WHERE "FlightDefinitionId" IS NULL;
    
    IF flights_without_definition > 0 THEN
        RAISE EXCEPTION 'Cannot complete migration: % flights without FlightDefinitionId', flights_without_definition;
    END IF;
    
    RAISE NOTICE 'Verification passed: All flights have FlightDefinitionId';
END $$;

-- Step 2: Drop old indexes
DROP INDEX IF EXISTS "IX_Flights_FlightNumber";
DROP INDEX IF EXISTS "IX_Flights_FlightNumber_DepartureTime";
DROP INDEX IF EXISTS "IX_Flights_RouteId";
DROP INDEX IF EXISTS "IX_Flights_AircraftId";

RAISE NOTICE 'Old indexes dropped';

-- Step 3: Drop old foreign key constraints
ALTER TABLE "Flights" DROP CONSTRAINT IF EXISTS "FK_Flights_Routes";
ALTER TABLE "Flights" DROP CONSTRAINT IF EXISTS "FK_Flights_Aircraft";

RAISE NOTICE 'Old foreign key constraints dropped';

-- Step 4: Drop old columns
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "FlightNumber";
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "RouteId";
ALTER TABLE "Flights" DROP COLUMN IF EXISTS "AircraftId";

RAISE NOTICE 'Old columns dropped';

-- Step 5: Verify final structure
SELECT 
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name = 'Flights'
ORDER BY ordinal_position;

-- Step 6: Show sample data
SELECT 
    f."Id",
    f."FlightDefinitionId",
    fd."FlightNumber",
    f."DepartureTime",
    f."ArrivalTime",
    f."ActualAircraftId",
    f."Status"
FROM "Flights" f
JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LIMIT 10;

SELECT 'Migration completed successfully!' as "Status";
