-- Check Flights table structure
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Flights'
ORDER BY ordinal_position;

-- Check if FlightDefinitionId column exists
SELECT EXISTS (
    SELECT 1 
    FROM information_schema.columns 
    WHERE table_name = 'Flights' 
    AND column_name = 'FlightDefinitionId'
) as "HasFlightDefinitionId";

-- Check if old columns still exist
SELECT 
    EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Flights' AND column_name = 'FlightNumber') as "HasFlightNumber",
    EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Flights' AND column_name = 'RouteId') as "HasRouteId",
    EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Flights' AND column_name = 'AircraftId') as "HasAircraftId",
    EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Flights' AND column_name = 'ActualAircraftId') as "HasActualAircraftId";

-- Sample data from Flights
SELECT 
    "Id",
    "FlightDefinitionId",
    "DepartureTime",
    "ArrivalTime",
    "Status"
FROM "Flights"
LIMIT 5;
