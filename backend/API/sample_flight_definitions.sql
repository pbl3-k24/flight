-- =====================================================
-- Sample Flight Definitions Data
-- =====================================================

-- Assumptions:
-- RouteId 1: HAN → SGN
-- RouteId 2: SGN → HAN
-- RouteId 3: HAN → DAD
-- RouteId 4: DAD → HAN
-- AircraftId 1, 2, 3: Available aircraft

-- Clear existing data (optional - be careful in production!)
-- TRUNCATE TABLE "FlightDefinitions" CASCADE;

-- =====================================================
-- HAN → SGN Routes
-- =====================================================

-- VN201: HAN → SGN, Morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN201', 1, 1, '06:00:00', '08:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN203: HAN → SGN, Mid-morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN203', 1, 2, '09:00:00', '11:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN205: HAN → SGN, Noon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN205', 1, 3, '12:00:00', '14:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN207: HAN → SGN, Afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN207', 1, 1, '15:00:00', '17:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN209: HAN → SGN, Evening flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN209', 1, 2, '18:00:00', '20:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN211: HAN → SGN, Night flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN211', 1, 3, '21:00:00', '23:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- SGN → HAN Routes
-- =====================================================

-- VN202: SGN → HAN, Morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN202', 2, 1, '06:00:00', '08:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN204: SGN → HAN, Mid-morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN204', 2, 2, '09:00:00', '11:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN206: SGN → HAN, Noon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN206', 2, 3, '12:00:00', '14:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN208: SGN → HAN, Afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN208', 2, 1, '15:00:00', '17:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VN210: SGN → HAN, Evening flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN210', 2, 2, '18:00:00', '20:15:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- HAN → DAD Routes
-- =====================================================

-- VN301: HAN → DAD, Morning flight (Mon-Fri only)
-- OperatingDays: 1+2+4+8+16 = 31 (Mon-Fri)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN301', 3, 1, '07:00:00', '08:20:00', 0, 31, TRUE, CURRENT_TIMESTAMP);

-- VN303: HAN → DAD, Afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN303', 3, 2, '14:00:00', '15:20:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- DAD → HAN Routes
-- =====================================================

-- VN302: DAD → HAN, Morning flight (Mon-Fri only)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN302', 4, 1, '09:00:00', '10:20:00', 0, 31, TRUE, CURRENT_TIMESTAMP);

-- VN304: DAD → HAN, Afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN304', 4, 2, '16:00:00', '17:20:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- VietJet Air Flights
-- =====================================================

-- VJ123: HAN → SGN, Budget morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VJ123', 1, 1, '05:30:00', '07:45:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VJ125: HAN → SGN, Budget afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VJ125', 1, 2, '13:30:00', '15:45:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VJ124: SGN → HAN, Budget morning flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VJ124', 2, 1, '05:30:00', '07:45:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- VJ126: SGN → HAN, Budget afternoon flight (Every day)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VJ126', 2, 2, '13:30:00', '15:45:00', 0, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- Overnight Flight Example
-- =====================================================

-- VN999: HAN → SGN, Red-eye flight (overnight)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN999', 1, 3, '23:30:00', '01:45:00', 1, 127, TRUE, CURRENT_TIMESTAMP);

-- =====================================================
-- Verification
-- =====================================================

SELECT 
    fd."FlightNumber",
    fd."DepartureTime",
    fd."ArrivalTime",
    fd."ArrivalOffsetDays",
    fd."OperatingDays",
    CASE 
        WHEN fd."OperatingDays" = 127 THEN 'Every day'
        WHEN fd."OperatingDays" = 31 THEN 'Mon-Fri'
        WHEN fd."OperatingDays" = 96 THEN 'Sat-Sun'
        ELSE 'Custom'
    END as "Schedule",
    fd."IsActive"
FROM "FlightDefinitions" fd
ORDER BY fd."FlightNumber";

-- Count by route
SELECT 
    r."Id" as "RouteId",
    COUNT(fd."Id") as "TotalDefinitions"
FROM "Routes" r
LEFT JOIN "FlightDefinitions" fd ON fd."RouteId" = r."Id"
GROUP BY r."Id"
ORDER BY r."Id";
