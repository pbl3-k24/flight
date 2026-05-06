-- Generate FlightSeatInventories from AircraftSeatTemplates
-- This creates seat inventory for all existing flights

-- First, check current state
SELECT 'Current state:' as info;
SELECT COUNT(*) as total_flights FROM "Flights";
SELECT COUNT(*) as total_seat_inventories FROM "FlightSeatInventories";
SELECT COUNT(*) as total_aircraft_templates FROM "AircraftSeatTemplates";

-- Delete existing inventories (if any)
DELETE FROM "FlightSeatInventories";

-- Generate FlightSeatInventories for all flights
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
    "IsDeleted",
    "DeletedAt"
)
SELECT 
    f."Id" as "FlightId",
    ast."SeatClassId",
    ast."DefaultSeatCount" as "TotalSeats",
    ast."DefaultSeatCount" as "AvailableSeats",
    0 as "HeldSeats",
    0 as "SoldSeats",
    ast."DefaultBasePrice" as "BasePrice",
    ast."DefaultBasePrice" as "CurrentPrice",
    NOW() as "CreatedAt",
    NOW() as "UpdatedAt",
    false as "IsDeleted",
    NULL as "DeletedAt"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "AircraftSeatTemplates" ast ON 
    COALESCE(f."ActualAircraftId", fd."DefaultAircraftId") = ast."AircraftId"
WHERE ast."IsDeleted" = false;

-- Show results
SELECT 'Generation complete!' as status;
SELECT COUNT(*) as total_inventories_created FROM "FlightSeatInventories";
SELECT 
    f."Id" as flight_id,
    fd."FlightNumber",
    COUNT(fsi."Id") as seat_classes
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "FlightSeatInventories" fsi ON f."Id" = fsi."FlightId"
GROUP BY f."Id", fd."FlightNumber"
ORDER BY f."Id"
LIMIT 10;
