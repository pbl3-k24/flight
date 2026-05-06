-- =====================================================
-- Migration: Add FlightDefinition and restructure Flight
-- =====================================================

-- Step 1: Create FlightDefinitions table
CREATE TABLE "FlightDefinitions" (
    "Id" SERIAL PRIMARY KEY,
    "FlightNumber" VARCHAR(20) NOT NULL UNIQUE,
    "RouteId" INTEGER NOT NULL,
    "DefaultAircraftId" INTEGER NOT NULL,
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "ArrivalOffsetDays" INTEGER NOT NULL DEFAULT 0,
    "OperatingDays" INTEGER NOT NULL DEFAULT 127,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    
    CONSTRAINT "FK_FlightDefinitions_Routes" 
        FOREIGN KEY ("RouteId") 
        REFERENCES "Routes"("Id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_FlightDefinitions_Aircraft" 
        FOREIGN KEY ("DefaultAircraftId") 
        REFERENCES "Aircraft"("Id") 
        ON DELETE RESTRICT
);

CREATE INDEX "IX_FlightDefinitions_FlightNumber" ON "FlightDefinitions"("FlightNumber");
CREATE INDEX "IX_FlightDefinitions_RouteId" ON "FlightDefinitions"("RouteId");
CREATE INDEX "IX_FlightDefinitions_IsActive" ON "FlightDefinitions"("IsActive");

-- Step 2: Migrate existing Flights data to FlightDefinitions
-- Extract unique flight patterns from existing flights
INSERT INTO "FlightDefinitions" (
    "FlightNumber",
    "RouteId", 
    "DefaultAircraftId",
    "DepartureTime",
    "ArrivalTime",
    "ArrivalOffsetDays",
    "OperatingDays",
    "IsActive",
    "CreatedAt"
)
SELECT DISTINCT
    f."FlightNumber",
    f."RouteId",
    f."AircraftId",
    f."DepartureTime"::TIME,
    f."ArrivalTime"::TIME,
    CASE 
        WHEN DATE(f."ArrivalTime") > DATE(f."DepartureTime") THEN 1
        ELSE 0
    END as "ArrivalOffsetDays",
    127 as "OperatingDays", -- Default: every day
    TRUE as "IsActive",
    MIN(f."CreatedAt") as "CreatedAt"
FROM "Flights" f
WHERE f."IsDeleted" = FALSE
GROUP BY 
    f."FlightNumber",
    f."RouteId",
    f."AircraftId",
    f."DepartureTime"::TIME,
    f."ArrivalTime"::TIME,
    CASE 
        WHEN DATE(f."ArrivalTime") > DATE(f."DepartureTime") THEN 1
        ELSE 0
    END;

-- Step 3: Add new column to Flights table
ALTER TABLE "Flights" ADD COLUMN "FlightDefinitionId" INTEGER NULL;
ALTER TABLE "Flights" ADD COLUMN "ActualAircraftId" INTEGER NULL;

-- Step 4: Populate FlightDefinitionId for existing flights
UPDATE "Flights" f
SET "FlightDefinitionId" = fd."Id"
FROM "FlightDefinitions" fd
WHERE f."FlightNumber" = fd."FlightNumber"
  AND f."RouteId" = fd."RouteId"
  AND f."AircraftId" = fd."DefaultAircraftId"
  AND f."DepartureTime"::TIME = fd."DepartureTime"
  AND f."ArrivalTime"::TIME = fd."ArrivalTime";

-- Step 5: Make FlightDefinitionId NOT NULL
ALTER TABLE "Flights" ALTER COLUMN "FlightDefinitionId" SET NOT NULL;

-- Step 6: Add foreign key constraint
ALTER TABLE "Flights" 
ADD CONSTRAINT "FK_Flights_FlightDefinitions" 
    FOREIGN KEY ("FlightDefinitionId") 
    REFERENCES "FlightDefinitions"("Id") 
    ON DELETE RESTRICT;

ALTER TABLE "Flights"
ADD CONSTRAINT "FK_Flights_ActualAircraft"
    FOREIGN KEY ("ActualAircraftId")
    REFERENCES "Aircraft"("Id")
    ON DELETE RESTRICT;

-- Step 7: Create new indexes
CREATE INDEX "IX_Flights_FlightDefinitionId" ON "Flights"("FlightDefinitionId");
CREATE INDEX "IX_Flights_FlightDefinitionId_DepartureTime" ON "Flights"("FlightDefinitionId", "DepartureTime");

-- Step 8: Drop old columns and constraints (CAREFUL!)
-- Uncomment these lines after verifying data migration is successful

-- DROP INDEX IF EXISTS "IX_Flights_FlightNumber";
-- DROP INDEX IF EXISTS "IX_Flights_FlightNumber_DepartureTime";
-- ALTER TABLE "Flights" DROP CONSTRAINT IF EXISTS "FK_Flights_Routes";
-- ALTER TABLE "Flights" DROP CONSTRAINT IF EXISTS "FK_Flights_Aircraft";
-- ALTER TABLE "Flights" DROP COLUMN "FlightNumber";
-- ALTER TABLE "Flights" DROP COLUMN "RouteId";
-- ALTER TABLE "Flights" DROP COLUMN "AircraftId";

-- Step 9: Update check constraint for Flight Status
ALTER TABLE "Flights" DROP CONSTRAINT IF EXISTS "CK_Flight_Status_Valid";
ALTER TABLE "Flights" ADD CONSTRAINT "CK_Flight_Status_Valid" 
    CHECK ("Status" IN (0, 1, 2, 3, 4));

-- =====================================================
-- Verification Queries
-- =====================================================

-- Check FlightDefinitions
SELECT COUNT(*) as "TotalFlightDefinitions" FROM "FlightDefinitions";

-- Check Flights with FlightDefinitionId
SELECT COUNT(*) as "FlightsWithDefinition" 
FROM "Flights" 
WHERE "FlightDefinitionId" IS NOT NULL;

-- Check Flights without FlightDefinitionId (should be 0)
SELECT COUNT(*) as "FlightsWithoutDefinition" 
FROM "Flights" 
WHERE "FlightDefinitionId" IS NULL;

-- Sample data
SELECT 
    fd."FlightNumber",
    fd."DepartureTime",
    fd."ArrivalTime",
    r."DepartureAirportId",
    r."ArrivalAirportId",
    COUNT(f."Id") as "TotalFlights"
FROM "FlightDefinitions" fd
JOIN "Routes" r ON fd."RouteId" = r."Id"
LEFT JOIN "Flights" f ON f."FlightDefinitionId" = fd."Id"
GROUP BY fd."Id", fd."FlightNumber", fd."DepartureTime", fd."ArrivalTime", r."DepartureAirportId", r."ArrivalAirportId"
ORDER BY fd."FlightNumber"
LIMIT 10;
