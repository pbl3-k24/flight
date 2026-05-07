-- Fix FlightNumberSuffix - add unique numbers for each flight

BEGIN;

-- Update empty FlightNumberSuffix with unique numbers based on Id
UPDATE "FlightTemplateDetails"
SET "FlightNumberSuffix" = LPAD("Id"::TEXT, 3, '0')
WHERE "FlightNumberSuffix" = '' OR "FlightNumberSuffix" IS NULL;

-- Show result
SELECT 
    "Id",
    "TemplateId",
    "FlightNumberPrefix",
    "FlightNumberSuffix",
    CONCAT("FlightNumberPrefix", "FlightNumberSuffix") as "FullFlightNumber",
    "DayOfWeek",
    "RouteId",
    "AircraftId"
FROM "FlightTemplateDetails"
ORDER BY "Id";

COMMIT;
