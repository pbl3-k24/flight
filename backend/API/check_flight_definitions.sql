-- Kiểm tra FlightDefinitions
SELECT COUNT(*) as "TotalFlightDefinitions" FROM "FlightDefinitions";

-- Xem FlightDefinitions
SELECT 
    fd."Id",
    fd."FlightNumber",
    fd."RouteId",
    r."DepartureAirportId",
    r."ArrivalAirportId",
    dep."Code" as "DepartureCode",
    arr."Code" as "ArrivalCode"
FROM "FlightDefinitions" fd
LEFT JOIN "Routes" r ON fd."RouteId" = r."Id"
LEFT JOIN "Airports" dep ON r."DepartureAirportId" = dep."Id"
LEFT JOIN "Airports" arr ON r."ArrivalAirportId" = arr."Id"
LIMIT 10;

-- Kiểm tra Flights không có FlightDefinitionId
SELECT 
    COUNT(*) as "FlightsWithoutDefinition"
FROM "Flights"
WHERE "FlightDefinitionId" IS NULL;
