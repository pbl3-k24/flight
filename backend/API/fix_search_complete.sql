-- Complete fix for flight search
-- Run this to create AircraftSeatTemplates and FlightSeatInventories

-- Step 1: Check current state
SELECT 'BEFORE:' as step;
SELECT COUNT(*) as aircraft_seat_templates FROM "AircraftSeatTemplates";
SELECT COUNT(*) as flight_seat_inventories FROM "FlightSeatInventories";
SELECT COUNT(*) as flights FROM "Flights";

-- Step 2: Create AircraftSeatTemplates if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "AircraftSeatTemplates" LIMIT 1) THEN
        -- Aircraft 1: Boeing 787-9
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (1, 1, 246, 1500000, false),
        (1, 2, 36, 4500000, false),
        (1, 3, 14, 8000000, false);

        -- Aircraft 2: Airbus A321neo
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (2, 1, 196, 1200000, false),
        (2, 2, 24, 3500000, false);

        -- Aircraft 3: Boeing 737 MAX 8
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (3, 1, 165, 1000000, false),
        (3, 2, 24, 3000000, false);

        -- Aircraft 4: Airbus A350-900
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (4, 1, 261, 1800000, false),
        (4, 2, 48, 5000000, false),
        (4, 3, 16, 9000000, false);

        -- Aircraft 5: Boeing 777-300ER
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (5, 1, 296, 2000000, false),
        (5, 2, 52, 5500000, false),
        (5, 3, 16, 10000000, false);

        -- Aircraft 6: Airbus A320neo
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (6, 1, 156, 900000, false),
        (6, 2, 24, 2800000, false);

        -- Aircraft 7: Boeing 787-10
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (7, 1, 270, 1700000, false),
        (7, 2, 44, 4800000, false),
        (7, 3, 16, 8500000, false);

        -- Aircraft 8: Airbus A330-300
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (8, 1, 221, 1600000, false),
        (8, 2, 42, 4500000, false),
        (8, 3, 14, 8000000, false);

        -- Aircraft 9: Boeing 737-800
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (9, 1, 150, 850000, false),
        (9, 2, 12, 2500000, false);

        -- Aircraft 10: Airbus A321
        INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted") VALUES 
        (10, 1, 196, 1100000, false),
        (10, 2, 24, 3200000, false);

        RAISE NOTICE 'AircraftSeatTemplates created';
    ELSE
        RAISE NOTICE 'AircraftSeatTemplates already exist';
    END IF;
END $$;

-- Step 3: Delete old FlightSeatInventories
DELETE FROM "FlightSeatInventories";

-- Step 4: Generate FlightSeatInventories
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
    CURRENT_TIMESTAMP as "CreatedAt",
    CURRENT_TIMESTAMP as "UpdatedAt",
    false as "IsDeleted",
    NULL as "DeletedAt"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "AircraftSeatTemplates" ast ON 
    COALESCE(f."ActualAircraftId", fd."DefaultAircraftId") = ast."AircraftId"
WHERE ast."IsDeleted" = false;

-- Step 5: Show results
SELECT 'AFTER:' as step;
SELECT COUNT(*) as aircraft_seat_templates FROM "AircraftSeatTemplates";
SELECT COUNT(*) as flight_seat_inventories FROM "FlightSeatInventories";
SELECT COUNT(*) as flights FROM "Flights";

-- Step 6: Sample check
SELECT 
    f."Id" as flight_id,
    fd."FlightNumber",
    COUNT(fsi."Id") as seat_classes,
    STRING_AGG(sc."Name", ', ' ORDER BY sc."Id") as classes
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "FlightSeatInventories" fsi ON f."Id" = fsi."FlightId"
LEFT JOIN "SeatClasses" sc ON fsi."SeatClassId" = sc."Id"
GROUP BY f."Id", fd."FlightNumber"
ORDER BY f."Id"
LIMIT 5;

SELECT '✓ Flight search should now work!' as status;
