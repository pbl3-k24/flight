-- ========================================
-- CREATE FLIGHT SEAT INVENTORIES ONLY
-- ========================================
-- Run this if AircraftSeatTemplates already exist

-- Step 1: Check current state
SELECT 'BEFORE:' as step;
SELECT COUNT(*) as aircraft_seat_templates FROM "AircraftSeatTemplates";
SELECT COUNT(*) as flight_seat_inventories FROM "FlightSeatInventories";
SELECT COUNT(*) as flights FROM "Flights";
SELECT COUNT(*) as flight_definitions FROM "FlightDefinitions";

-- Step 2: Delete old FlightSeatInventories
DELETE FROM "FlightSeatInventories";

-- Step 3: Generate FlightSeatInventories
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

-- Step 4: Verify results
SELECT 'AFTER:' as step;
SELECT COUNT(*) as flight_seat_inventories FROM "FlightSeatInventories";

-- Step 5: Sample check - show first 10 flights with their seat inventories
SELECT 
    f."Id" as flight_id,
    fd."FlightNumber",
    fd."DefaultAircraftId" as aircraft_id,
    f."DepartureTime",
    COUNT(fsi."Id") as seat_inventory_count,
    STRING_AGG(sc."Name" || ': ' || fsi."AvailableSeats" || ' seats @ ' || fsi."CurrentPrice", ', ' ORDER BY sc."Id") as inventory_details
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "FlightSeatInventories" fsi ON f."Id" = fsi."FlightId"
LEFT JOIN "SeatClasses" sc ON fsi."SeatClassId" = sc."Id"
GROUP BY f."Id", fd."FlightNumber", fd."DefaultAircraftId", f."DepartureTime"
ORDER BY f."DepartureTime"
LIMIT 10;

-- Step 6: Summary by aircraft
SELECT 
    fd."DefaultAircraftId" as aircraft_id,
    a."Model" as aircraft_model,
    COUNT(DISTINCT f."Id") as total_flights,
    COUNT(fsi."Id") as total_seat_inventories
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "Aircraft" a ON fd."DefaultAircraftId" = a."Id"
LEFT JOIN "FlightSeatInventories" fsi ON f."Id" = fsi."FlightId"
GROUP BY fd."DefaultAircraftId", a."Model"
ORDER BY fd."DefaultAircraftId";

SELECT '✓ DONE! Flight search should now work.' as status;
