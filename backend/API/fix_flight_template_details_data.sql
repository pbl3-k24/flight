-- Fix FlightTemplateDetails data issues
-- Populate FlightNumberPrefix and FlightNumberSuffix from old FlightNumber if exists

BEGIN;

-- Check if there are any rows
SELECT COUNT(*) as "TotalRows" FROM "FlightTemplateDetails";

-- Show current data
SELECT 
    "Id",
    "TemplateId",
    "FlightNumberPrefix",
    "FlightNumberSuffix",
    "DayOfWeek",
    "RouteId",
    "AircraftId"
FROM "FlightTemplateDetails"
LIMIT 10;

-- If FlightNumberPrefix or FlightNumberSuffix is NULL, set default values
UPDATE "FlightTemplateDetails"
SET 
    "FlightNumberPrefix" = COALESCE("FlightNumberPrefix", 'VN'),
    "FlightNumberSuffix" = COALESCE("FlightNumberSuffix", LPAD("Id"::TEXT, 3, '0'))
WHERE "FlightNumberPrefix" IS NULL OR "FlightNumberSuffix" IS NULL;

-- Show updated data
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
LIMIT 10;

COMMIT;
