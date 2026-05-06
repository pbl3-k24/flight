-- =====================================================
-- CLEAN RECREATION: Drop and Recreate Flights with FlightDefinition
-- =====================================================
-- This script completely removes old data and recreates tables

-- Step 1: Drop all dependent data (in correct order)
DELETE FROM "Tickets";
DELETE FROM "RefundRequests";
DELETE FROM "Payments";
DELETE FROM "BookingPassengers";
DELETE FROM "BookingServices";
DELETE FROM "Bookings";
DELETE FROM "FlightSeatInventories";

-- Step 2: Drop Flights table completely
DROP TABLE IF EXISTS "Flights" CASCADE;

-- Step 3: Drop FlightDefinitions table if exists
DROP TABLE IF EXISTS "FlightDefinitions" CASCADE;

-- Step 4: Create FlightDefinitions table (NEW)
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

-- Step 5: Create Flights table (NEW STRUCTURE)
CREATE TABLE "Flights" (
    "Id" SERIAL PRIMARY KEY,
    "FlightDefinitionId" INTEGER NOT NULL,
    "DepartureTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ArrivalTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ActualAircraftId" INTEGER NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" INTEGER NULL,
    "UpdatedBy" INTEGER NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    "Version" INTEGER NOT NULL DEFAULT 0,
    
    CONSTRAINT "FK_Flights_FlightDefinitions" 
        FOREIGN KEY ("FlightDefinitionId") 
        REFERENCES "FlightDefinitions"("Id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_Flights_ActualAircraft"
        FOREIGN KEY ("ActualAircraftId")
        REFERENCES "Aircraft"("Id")
        ON DELETE RESTRICT,
        
    CONSTRAINT "CK_Flight_Status_Valid" 
        CHECK ("Status" IN (0, 1, 2, 3, 4))
);

CREATE INDEX "IX_Flights_FlightDefinitionId" ON "Flights"("FlightDefinitionId");
CREATE INDEX "IX_Flights_DepartureTime" ON "Flights"("DepartureTime");
CREATE INDEX "IX_Flights_FlightDefinitionId_DepartureTime" ON "Flights"("FlightDefinitionId", "DepartureTime");
CREATE INDEX "IX_Flights_Status" ON "Flights"("Status");
CREATE INDEX "IX_Flights_IsDeleted" ON "Flights"("IsDeleted");

-- Step 6: Reset sequences
SELECT setval(pg_get_serial_sequence('"FlightDefinitions"', 'Id'), 1, false);
SELECT setval(pg_get_serial_sequence('"Flights"', 'Id'), 1, false);
SELECT setval(pg_get_serial_sequence('"FlightSeatInventories"', 'Id'), 1, false);
SELECT setval(pg_get_serial_sequence('"Bookings"', 'Id'), 1, false);
SELECT setval(pg_get_serial_sequence('"Payments"', 'Id'), 1, false);
SELECT setval(pg_get_serial_sequence('"Tickets"', 'Id'), 1, false);

-- Verification
SELECT 'Tables recreated successfully!' as "Status";

SELECT 
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name IN ('FlightDefinitions', 'Flights')
ORDER BY table_name, ordinal_position;
