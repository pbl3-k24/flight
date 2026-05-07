-- =====================================================
-- VERIFY SEEDED DATA - Kiểm tra dữ liệu sau khi seed
-- =====================================================

\echo '========================================'
\echo 'VERIFICATION REPORT'
\echo '========================================'
\echo ''

-- 1. Tổng số records
\echo '1. TOTAL RECORDS:'
\echo '----------------------------------------'
SELECT 
    'FlightDefinitions' as "Table",
    COUNT(*) as "Count",
    CASE 
        WHEN COUNT(*) >= 15 THEN '✓ OK'
        ELSE '✗ TOO FEW'
    END as "Status"
FROM "FlightDefinitions"
UNION ALL
SELECT 
    'Flights (with Definition)',
    COUNT(*),
    CASE 
        WHEN COUNT(*) >= 400 THEN '✓ OK'
        WHEN COUNT(*) >= 200 THEN '⚠ LOW'
        ELSE '✗ TOO FEW'
    END
FROM "Flights"
WHERE "FlightDefinitionId" IS NOT NULL
UNION ALL
SELECT 
    'FlightSeatInventories',
    COUNT(*),
    CASE 
        WHEN COUNT(*) >= 1200 THEN '✓ OK'
        WHEN COUNT(*) >= 600 THEN '⚠ LOW'
        ELSE '✗ TOO FEW'
    END
FROM "FlightSeatInventories"
WHERE "FlightId" IN (
    SELECT "Id" FROM "Flights" WHERE "FlightDefinitionId" IS NOT NULL
);

\echo ''
\echo '2. FLIGHTS BY DATE (First 10 days):'
\echo '----------------------------------------'
SELECT 
    TO_CHAR(DATE(f."DepartureTime"), 'YYYY-MM-DD Day') as "Date",
    COUNT(*) as "Flights",
    CASE 
        WHEN COUNT(*) >= 15 THEN '✓ OK'
        WHEN COUNT(*) >= 10 THEN '⚠ LOW'
        ELSE '✗ TOO FEW'
    END as "Status"
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL
GROUP BY DATE(f."DepartureTime")
ORDER BY DATE(f."DepartureTime")
LIMIT 10;

\echo ''
\echo '3. FLIGHTS BY DEFINITION:'
\echo '----------------------------------------'
SELECT 
    fd."FlightNumber",
    fd."DepartureTime"::TEXT as "Time",
    CASE fd."OperatingDays"
        WHEN 127 THEN 'Every day'
        WHEN 31 THEN 'Mon-Fri'
        WHEN 96 THEN 'Sat-Sun'
        ELSE 'Custom'
    END as "Schedule",
    COUNT(f."Id") as "Flights",
    CASE 
        WHEN COUNT(f."Id") >= 20 THEN '✓ OK'
        WHEN COUNT(f."Id") >= 10 THEN '⚠ LOW'
        ELSE '✗ TOO FEW'
    END as "Status"
FROM "FlightDefinitions" fd
LEFT JOIN "Flights" f ON f."FlightDefinitionId" = fd."Id"
GROUP BY fd."FlightNumber", fd."DepartureTime", fd."OperatingDays"
ORDER BY fd."FlightNumber";

\echo ''
\echo '4. SAMPLE FLIGHTS TODAY:'
\echo '----------------------------------------'
SELECT 
    fd."FlightNumber",
    TO_CHAR(f."DepartureTime", 'HH24:MI') as "Departure",
    TO_CHAR(f."ArrivalTime", 'HH24:MI') as "Arrival",
    r."DepartureAirportId" || ' → ' || r."ArrivalAirportId" as "Route",
    COUNT(fsi."Id") as "SeatClasses"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "Routes" r ON fd."RouteId" = r."Id"
LEFT JOIN "FlightSeatInventories" fsi ON fsi."FlightId" = f."Id"
WHERE DATE(f."DepartureTime") = CURRENT_DATE
GROUP BY fd."FlightNumber", f."DepartureTime", f."ArrivalTime", r."DepartureAirportId", r."ArrivalAirportId"
ORDER BY f."DepartureTime"
LIMIT 10;

\echo ''
\echo '5. SEAT INVENTORY SAMPLE:'
\echo '----------------------------------------'
SELECT 
    fd."FlightNumber",
    sc."Name" as "SeatClass",
    fsi."TotalSeats",
    fsi."AvailableSeats",
    fsi."BasePrice",
    CASE 
        WHEN fsi."AvailableSeats" = fsi."TotalSeats" THEN '✓ All Available'
        ELSE '⚠ Some Booked'
    END as "Status"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "FlightSeatInventories" fsi ON fsi."FlightId" = f."Id"
INNER JOIN "SeatClasses" sc ON sc."Id" = fsi."SeatClassId"
WHERE DATE(f."DepartureTime") = CURRENT_DATE
ORDER BY f."DepartureTime", fd."FlightNumber", sc."Priority"
LIMIT 15;

\echo ''
\echo '6. ROUTES COVERAGE:'
\echo '----------------------------------------'
SELECT 
    r."Id" as "RouteId",
    da."Code" || ' → ' || aa."Code" as "Route",
    COUNT(DISTINCT fd."Id") as "Definitions",
    COUNT(f."Id") as "Flights",
    CASE 
        WHEN COUNT(DISTINCT fd."Id") >= 2 THEN '✓ Good'
        WHEN COUNT(DISTINCT fd."Id") >= 1 THEN '⚠ Limited'
        ELSE '✗ None'
    END as "Status"
FROM "Routes" r
INNER JOIN "Airports" da ON da."Id" = r."DepartureAirportId"
INNER JOIN "Airports" aa ON aa."Id" = r."ArrivalAirportId"
LEFT JOIN "FlightDefinitions" fd ON fd."RouteId" = r."Id"
LEFT JOIN "Flights" f ON f."FlightDefinitionId" = fd."Id"
GROUP BY r."Id", da."Code", aa."Code"
ORDER BY r."Id";

\echo ''
\echo '7. DATE RANGE:'
\echo '----------------------------------------'
SELECT 
    'First Flight' as "Type",
    TO_CHAR(MIN(f."DepartureTime"), 'YYYY-MM-DD HH24:MI') as "DateTime"
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL
UNION ALL
SELECT 
    'Last Flight',
    TO_CHAR(MAX(f."DepartureTime"), 'YYYY-MM-DD HH24:MI')
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL
UNION ALL
SELECT 
    'Days Coverage',
    (MAX(DATE(f."DepartureTime")) - MIN(DATE(f."DepartureTime")) + 1)::TEXT || ' days'
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL;

\echo ''
\echo '8. DATA INTEGRITY CHECKS:'
\echo '----------------------------------------'
SELECT 
    'Flights without Seat Inventory' as "Check",
    COUNT(*) as "Count",
    CASE 
        WHEN COUNT(*) = 0 THEN '✓ OK'
        ELSE '✗ ISSUE'
    END as "Status"
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM "FlightSeatInventories" fsi 
    WHERE fsi."FlightId" = f."Id"
)
UNION ALL
SELECT 
    'Definitions without Flights',
    COUNT(*),
    CASE 
        WHEN COUNT(*) = 0 THEN '✓ OK'
        ELSE '⚠ WARNING'
    END
FROM "FlightDefinitions" fd
WHERE NOT EXISTS (
    SELECT 1 FROM "Flights" f 
    WHERE f."FlightDefinitionId" = fd."Id"
)
UNION ALL
SELECT 
    'Flights with Invalid Aircraft',
    COUNT(*),
    CASE 
        WHEN COUNT(*) = 0 THEN '✓ OK'
        ELSE '✗ ISSUE'
    END
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
WHERE NOT EXISTS (
    SELECT 1 FROM "Aircraft" a 
    WHERE a."Id" = fd."DefaultAircraftId"
);

\echo ''
\echo '========================================'
\echo 'VERIFICATION COMPLETE'
\echo '========================================'
\echo ''
\echo 'Legend:'
\echo '  ✓ OK      - Everything is good'
\echo '  ⚠ WARNING - Acceptable but could be better'
\echo '  ✗ ISSUE   - Needs attention'
\echo ''
