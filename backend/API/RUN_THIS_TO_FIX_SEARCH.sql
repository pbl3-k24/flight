-- ========================================
-- RUN THIS SCRIPT TO FIX FLIGHT SEARCH
-- ========================================
-- This creates AircraftSeatTemplates and FlightSeatInventories
-- Run in pgAdmin or any PostgreSQL client

-- Step 0: Check existing aircraft
SELECT 'Existing Aircraft:' as info;
SELECT "Id", "Model", "RegistrationNumber" FROM "Aircraft" ORDER BY "Id";

-- Step 1: Create AircraftSeatTemplates (only for existing aircraft)
DELETE FROM "AircraftSeatTemplates";

INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
-- Aircraft 1: Boeing 787-9
(1, 1, 246, 1500000, false),
(1, 2, 36, 4500000, false),
(1, 3, 14, 8000000, false),
-- Aircraft 2: Airbus A321neo
(2, 1, 196, 1200000, false),
(2, 2, 24, 3500000, false),
-- Aircraft 3: Boeing 737 MAX 8
(3, 1, 165, 1000000, false),
(3, 2, 24, 3000000, false),
-- Aircraft 4: Airbus A350-900
(4, 1, 261, 1800000, false),
(4, 2, 48, 5000000, false),
(4, 3, 16, 9000000, false),
-- Aircraft 5: Boeing 777-300ER
(5, 1, 296, 2000000, false),
(5, 2, 52, 5500000, false),
(5, 3, 16, 10000000, false);

-- Step 2: Generate FlightSeatInventories
DELETE FROM "FlightSeatInventories";

INSERT INTO "FlightSeatInventories" (
    "FlightId",
    "SeatClassId",
    "TotalSeats",
    "AvailableSeats",
    "HeldSeats",
    "SoldSeats",
    "BasePrice",
    "CurrentPrice",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted"
)
SELECT 
    f."Id",
    ast."SeatClassId",
    ast."DefaultSeatCount",
    ast."DefaultSeatCount",
    0,
    0,
    ast."DefaultBasePrice",
    ast."DefaultBasePrice",
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    false
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "AircraftSeatTemplates" ast ON fd."DefaultAircraftId" = ast."AircraftId"
WHERE ast."IsDeleted" = false;

-- Step 3: Verify
SELECT 'Results:' as info;
SELECT COUNT(*) as aircraft_seat_templates FROM "AircraftSeatTemplates";
SELECT COUNT(*) as flight_seat_inventories FROM "FlightSeatInventories";
SELECT COUNT(*) as flights FROM "Flights";

-- Step 4: Sample check
SELECT 
    f."Id",
    fd."FlightNumber",
    fd."DefaultAircraftId",
    COUNT(fsi."Id") as seat_inventory_count
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "FlightSeatInventories" fsi ON f."Id" = fsi."FlightId"
GROUP BY f."Id", fd."FlightNumber", fd."DefaultAircraftId"
ORDER BY f."Id"
LIMIT 10;

SELECT '✓ DONE! Flight search should now work.' as status;
