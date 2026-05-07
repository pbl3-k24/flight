-- Kiểm tra số lượng flights trong database
SELECT COUNT(*) as "TotalFlights" FROM "Flights";

-- Xem 10 flights đầu tiên
SELECT 
    f."Id",
    f."FlightDefinitionId",
    f."DepartureTime",
    f."ArrivalTime",
    f."Status",
    f."IsDeleted",
    fd."FlightNumber",
    dep."Code" as "DepartureAirport",
    arr."Code" as "ArrivalAirport"
FROM "Flights" f
LEFT JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
LEFT JOIN "Routes" r ON fd."RouteId" = r."Id"
LEFT JOIN "Airports" dep ON r."DepartureAirportId" = dep."Id"
LEFT JOIN "Airports" arr ON r."ArrivalAirportId" = arr."Id"
ORDER BY f."DepartureTime"
LIMIT 10;

-- Kiểm tra flights có IsDeleted = true
SELECT COUNT(*) as "DeletedFlights" 
FROM "Flights" 
WHERE "IsDeleted" = true;

-- Kiểm tra flights theo status
SELECT 
    "Status",
    COUNT(*) as "Count"
FROM "Flights"
GROUP BY "Status"
ORDER BY "Status";
