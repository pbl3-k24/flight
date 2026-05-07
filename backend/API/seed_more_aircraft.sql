-- =====================================================
-- THÊM NHIỀU MÁY BAY MỚI
-- =====================================================

BEGIN;

-- =====================================================
-- BƯỚC 1: Thêm Aircraft mới
-- =====================================================

INSERT INTO "Aircraft" (
    "RegistrationNumber",
    "Model",
    "TotalSeats",
    "IsActive",
    "IsDeleted"
)
VALUES
    -- Boeing 787 Dreamliner (Wide-body, long-haul)
    ('VN-A861', 'Boeing 787-9', 274, TRUE, FALSE),
    ('VN-A862', 'Boeing 787-9', 274, TRUE, FALSE),
    ('VN-A863', 'Boeing 787-10', 367, TRUE, FALSE),
    
    -- Boeing 777 (Wide-body, long-haul)
    ('VN-A871', 'Boeing 777-200ER', 353, TRUE, FALSE),
    ('VN-A872', 'Boeing 777-200ER', 353, TRUE, FALSE),
    
    -- Airbus A350 (Wide-body, long-haul)
    ('VN-A891', 'Airbus A350-900', 305, TRUE, FALSE),
    ('VN-A892', 'Airbus A350-900', 305, TRUE, FALSE),
    ('VN-A893', 'Airbus A350-1000', 410, TRUE, FALSE),
    
    -- Airbus A330 (Wide-body, medium-haul)
    ('VN-A881', 'Airbus A330-200', 269, TRUE, FALSE),
    ('VN-A882', 'Airbus A330-300', 290, TRUE, FALSE),
    
    -- Boeing 737 MAX (Narrow-body, short-medium haul)
    ('VN-A201', 'Boeing 737 MAX 8', 186, TRUE, FALSE),
    ('VN-A202', 'Boeing 737 MAX 8', 186, TRUE, FALSE),
    ('VN-A203', 'Boeing 737 MAX 8', 186, TRUE, FALSE),
    ('VN-A204', 'Boeing 737 MAX 9', 220, TRUE, FALSE),
    
    -- Airbus A320neo family (Narrow-body, short-medium haul)
    ('VN-A301', 'Airbus A320neo', 180, TRUE, FALSE),
    ('VN-A302', 'Airbus A320neo', 180, TRUE, FALSE),
    ('VN-A303', 'Airbus A321neo', 220, TRUE, FALSE),
    ('VN-A304', 'Airbus A321neo', 220, TRUE, FALSE),
    ('VN-A305', 'Airbus A321neo', 220, TRUE, FALSE),
    
    -- ATR 72 (Turboprop, regional)
    ('VN-B501', 'ATR 72-600', 70, TRUE, FALSE),
    ('VN-B502', 'ATR 72-600', 70, TRUE, FALSE),
    ('VN-B503', 'ATR 72-600', 70, TRUE, FALSE)
ON CONFLICT ("RegistrationNumber") DO NOTHING;

-- =====================================================
-- BƯỚC 2: Tạo AircraftSeatTemplates cho máy bay mới
-- =====================================================

-- Boeing 787-9 (274 ghế: 28 Business + 246 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 28
        WHEN sc."Name" = 'Economy' THEN 246
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 8500000
        WHEN sc."Name" = 'Economy' THEN 3500000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Boeing 787-9'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Boeing 787-10 (367 ghế: 38 Business + 329 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 38
        WHEN sc."Name" = 'Economy' THEN 329
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 9000000
        WHEN sc."Name" = 'Economy' THEN 3800000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Boeing 787-10'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Boeing 777-200ER (353 ghế: 35 Business + 318 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 35
        WHEN sc."Name" = 'Economy' THEN 318
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 9500000
        WHEN sc."Name" = 'Economy' THEN 4000000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Boeing 777-200ER'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A350-900 (305 ghế: 29 Business + 276 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 29
        WHEN sc."Name" = 'Economy' THEN 276
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 8800000
        WHEN sc."Name" = 'Economy' THEN 3600000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A350-900'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A350-1000 (410 ghế: 40 Business + 370 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 40
        WHEN sc."Name" = 'Economy' THEN 370
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 9200000
        WHEN sc."Name" = 'Economy' THEN 3900000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A350-1000'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A330-200 (269 ghế: 24 Business + 245 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 24
        WHEN sc."Name" = 'Economy' THEN 245
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 7500000
        WHEN sc."Name" = 'Economy' THEN 3200000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A330-200'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A330-300 (290 ghế: 30 Business + 260 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 30
        WHEN sc."Name" = 'Economy' THEN 260
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 8000000
        WHEN sc."Name" = 'Economy' THEN 3400000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A330-300'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Boeing 737 MAX 8 (186 ghế: 16 Business + 170 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 16
        WHEN sc."Name" = 'Economy' THEN 170
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 3500000
        WHEN sc."Name" = 'Economy' THEN 1800000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Boeing 737 MAX 8'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Boeing 737 MAX 9 (220 ghế: 20 Business + 200 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 20
        WHEN sc."Name" = 'Economy' THEN 200
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 3800000
        WHEN sc."Name" = 'Economy' THEN 2000000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Boeing 737 MAX 9'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A320neo (180 ghế: 12 Business + 168 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 12
        WHEN sc."Name" = 'Economy' THEN 168
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 3200000
        WHEN sc."Name" = 'Economy' THEN 1700000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A320neo'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- Airbus A321neo (220 ghế: 20 Business + 200 Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    CASE 
        WHEN sc."Name" = 'Business' THEN 20
        WHEN sc."Name" = 'Economy' THEN 200
    END,
    CASE 
        WHEN sc."Name" = 'Business' THEN 3600000
        WHEN sc."Name" = 'Economy' THEN 1900000
    END,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'Airbus A321neo'
AND sc."Name" IN ('Business', 'Economy')
ON CONFLICT DO NOTHING;

-- ATR 72-600 (70 ghế: chỉ Economy)
INSERT INTO "AircraftSeatTemplates" (
    "AircraftId",
    "SeatClassId",
    "DefaultSeatCount",
    "DefaultBasePrice",
    "IsDeleted"
)
SELECT 
    a."Id",
    sc."Id",
    70,
    1200000,
    FALSE
FROM "Aircraft" a
CROSS JOIN "SeatClasses" sc
WHERE a."Model" = 'ATR 72-600'
AND sc."Name" = 'Economy'
ON CONFLICT DO NOTHING;

COMMIT;

-- =====================================================
-- BƯỚC 3: Verification
-- =====================================================

\echo ''
\echo '========================================'
\echo 'AIRCRAFT SEEDED SUCCESSFULLY'
\echo '========================================'
\echo ''

-- Đếm số aircraft
SELECT COUNT(*) as "TotalAircraft" FROM "Aircraft";

-- Xem aircraft mới
SELECT 
    "Id",
    "RegistrationNumber",
    "Model",
    "TotalSeats",
    "IsActive"
FROM "Aircraft"
ORDER BY "Id" DESC
LIMIT 25;

\echo ''
\echo 'Seat Templates:'
\echo ''

-- Xem seat templates
SELECT 
    a."RegistrationNumber",
    a."Model",
    sc."Name" as "SeatClass",
    ast."DefaultSeatCount",
    ast."DefaultBasePrice"
FROM "AircraftSeatTemplates" ast
INNER JOIN "Aircraft" a ON ast."AircraftId" = a."Id"
INNER JOIN "SeatClasses" sc ON ast."SeatClassId" = sc."Id"
WHERE a."RegistrationNumber" LIKE 'VN-A8%' 
   OR a."RegistrationNumber" LIKE 'VN-A2%'
   OR a."RegistrationNumber" LIKE 'VN-A3%'
   OR a."RegistrationNumber" LIKE 'VN-B5%'
ORDER BY a."Model", sc."Priority";

\echo ''
\echo '========================================'
\echo 'SUMMARY BY MODEL'
\echo '========================================'
\echo ''

SELECT 
    a."Model",
    COUNT(DISTINCT a."Id") as "Count",
    AVG(a."TotalSeats")::INTEGER as "AvgSeats"
FROM "Aircraft" a
GROUP BY a."Model"
ORDER BY a."Model";
