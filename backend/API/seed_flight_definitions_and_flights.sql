-- =====================================================
-- COMPLETE SEED DATA: FlightDefinitions + Flights + FlightSeatInventory
-- Tạo dữ liệu mẫu đầy đủ cho 30 ngày tới
-- =====================================================

-- =====================================================
-- BƯỚC 1: Tạo bảng FlightDefinitions (nếu chưa có)
-- =====================================================

CREATE TABLE IF NOT EXISTS "FlightDefinitions" (
    "Id" SERIAL PRIMARY KEY,
    "FlightNumber" VARCHAR(20) NOT NULL,
    "RouteId" INTEGER NOT NULL,
    "DefaultAircraftId" INTEGER NOT NULL,
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "ArrivalOffsetDays" INTEGER NOT NULL DEFAULT 0,
    "OperatingDays" INTEGER NOT NULL DEFAULT 127,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    
    CONSTRAINT "FK_FlightDefinitions_Routes" 
        FOREIGN KEY ("RouteId") REFERENCES "Routes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FlightDefinitions_Aircraft" 
        FOREIGN KEY ("DefaultAircraftId") REFERENCES "Aircraft"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_FlightDefinitions_FlightNumber" 
        UNIQUE ("FlightNumber")
);

CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_RouteId" ON "FlightDefinitions"("RouteId");
CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_DefaultAircraftId" ON "FlightDefinitions"("DefaultAircraftId");
CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_FlightNumber" ON "FlightDefinitions"("FlightNumber");

-- =====================================================
-- BƯỚC 2: Xóa dữ liệu cũ (nếu có)
-- =====================================================

DELETE FROM "FlightSeatInventories" WHERE "FlightId" IN (
    SELECT "Id" FROM "Flights" WHERE "FlightDefinitionId" IS NOT NULL
);
DELETE FROM "Flights" WHERE "FlightDefinitionId" IS NOT NULL;
DELETE FROM "FlightDefinitions";

-- =====================================================
-- BƯỚC 3: Insert FlightDefinitions
-- =====================================================

-- HAN → SGN (RouteId = 1)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES 
    ('VN201', 1, 1, '06:00:00', '08:15:00', 0, 127, TRUE, NOW()),
    ('VN203', 1, 2, '09:00:00', '11:15:00', 0, 127, TRUE, NOW()),
    ('VN205', 1, 3, '12:00:00', '14:15:00', 0, 127, TRUE, NOW()),
    ('VN207', 1, 1, '15:00:00', '17:15:00', 0, 127, TRUE, NOW()),
    ('VN209', 1, 2, '18:00:00', '20:15:00', 0, 127, TRUE, NOW()),
    ('VN211', 1, 3, '21:00:00', '23:15:00', 0, 127, TRUE, NOW()),
    ('VJ123', 1, 1, '05:30:00', '07:45:00', 0, 127, TRUE, NOW()),
    ('VJ125', 1, 2, '13:30:00', '15:45:00', 0, 127, TRUE, NOW());

-- SGN → HAN (RouteId = 2)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES 
    ('VN202', 2, 1, '06:00:00', '08:15:00', 0, 127, TRUE, NOW()),
    ('VN204', 2, 2, '09:00:00', '11:15:00', 0, 127, TRUE, NOW()),
    ('VN206', 2, 3, '12:00:00', '14:15:00', 0, 127, TRUE, NOW()),
    ('VN208', 2, 1, '15:00:00', '17:15:00', 0, 127, TRUE, NOW()),
    ('VN210', 2, 2, '18:00:00', '20:15:00', 0, 127, TRUE, NOW()),
    ('VJ124', 2, 1, '05:30:00', '07:45:00', 0, 127, TRUE, NOW()),
    ('VJ126', 2, 2, '13:30:00', '15:45:00', 0, 127, TRUE, NOW());

-- HAN → DAD (RouteId = 3)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES 
    ('VN301', 3, 1, '07:00:00', '08:20:00', 0, 31, TRUE, NOW()),  -- Mon-Fri only
    ('VN303', 3, 2, '14:00:00', '15:20:00', 0, 127, TRUE, NOW()); -- Every day

-- DAD → HAN (RouteId = 4)
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES 
    ('VN302', 4, 1, '09:00:00', '10:20:00', 0, 31, TRUE, NOW()),  -- Mon-Fri only
    ('VN304', 4, 2, '16:00:00', '17:20:00', 0, 127, TRUE, NOW()); -- Every day

-- Overnight flight example
INSERT INTO "FlightDefinitions" ("FlightNumber", "RouteId", "DefaultAircraftId", "DepartureTime", "ArrivalTime", "ArrivalOffsetDays", "OperatingDays", "IsActive", "CreatedAt")
VALUES ('VN999', 1, 3, '23:30:00', '01:45:00', 1, 127, TRUE, NOW());

-- =====================================================
-- BƯỚC 4: Tạo Flights từ FlightDefinitions (30 ngày tới)
-- =====================================================

DO $$
DECLARE
    v_definition RECORD;
    v_date DATE;
    v_day_of_week INTEGER;
    v_day_bit INTEGER;
    v_departure_datetime TIMESTAMP WITH TIME ZONE;
    v_arrival_datetime TIMESTAMP WITH TIME ZONE;
    v_flight_id INTEGER;
    v_seat_class RECORD;
    v_aircraft_seat RECORD;
BEGIN
    -- Loop qua 30 ngày tới
    FOR i IN 0..29 LOOP
        v_date := CURRENT_DATE + i;
        v_day_of_week := EXTRACT(DOW FROM v_date); -- 0=Sunday, 1=Monday, ..., 6=Saturday
        
        -- Convert to bit: Monday=1, Tuesday=2, Wednesday=4, Thursday=8, Friday=16, Saturday=32, Sunday=64
        v_day_bit := CASE v_day_of_week
            WHEN 1 THEN 1   -- Monday
            WHEN 2 THEN 2   -- Tuesday
            WHEN 3 THEN 4   -- Wednesday
            WHEN 4 THEN 8   -- Thursday
            WHEN 5 THEN 16  -- Friday
            WHEN 6 THEN 32  -- Saturday
            WHEN 0 THEN 64  -- Sunday
        END;
        
        -- Loop qua tất cả FlightDefinitions
        FOR v_definition IN 
            SELECT * FROM "FlightDefinitions" WHERE "IsActive" = TRUE
        LOOP
            -- Kiểm tra xem chuyến bay có hoạt động vào ngày này không
            IF (v_definition."OperatingDays" & v_day_bit) > 0 THEN
                
                -- Tính departure datetime
                v_departure_datetime := v_date + v_definition."DepartureTime";
                
                -- Tính arrival datetime (có thể qua ngày)
                v_arrival_datetime := v_date + v_definition."ArrivalTime" + 
                    (v_definition."ArrivalOffsetDays" || ' days')::INTERVAL;
                
                -- Insert Flight
                INSERT INTO "Flights" (
                    "FlightDefinitionId",
                    "DepartureTime",
                    "ArrivalTime",
                    "ActualAircraftId",
                    "Status",
                    "CreatedAt",
                    "UpdatedAt"
                )
                VALUES (
                    v_definition."Id",
                    v_departure_datetime,
                    v_arrival_datetime,
                    NULL, -- Use default aircraft from definition
                    0, -- Scheduled
                    NOW(),
                    NOW()
                )
                RETURNING "Id" INTO v_flight_id;
                
                -- Tạo FlightSeatInventory từ AircraftSeatTemplates
                FOR v_aircraft_seat IN
                    SELECT 
                        ast."SeatClassId",
                        ast."DefaultSeatCount",
                        ast."DefaultBasePrice"
                    FROM "AircraftSeatTemplates" ast
                    WHERE ast."AircraftId" = v_definition."DefaultAircraftId"
                    AND ast."IsDeleted" = FALSE
                LOOP
                    INSERT INTO "FlightSeatInventories" (
                        "FlightId",
                        "SeatClassId",
                        "TotalSeats",
                        "AvailableSeats",
                        "BasePrice",
                        "CurrentPrice",
                        "CreatedAt",
                        "UpdatedAt"
                    )
                    VALUES (
                        v_flight_id,
                        v_aircraft_seat."SeatClassId",
                        v_aircraft_seat."DefaultSeatCount",
                        v_aircraft_seat."DefaultSeatCount", -- All available initially
                        v_aircraft_seat."DefaultBasePrice",
                        v_aircraft_seat."DefaultBasePrice", -- CurrentPrice = BasePrice initially
                        NOW(),
                        NOW()
                    );
                END LOOP;
                
            END IF;
        END LOOP;
    END LOOP;
    
    RAISE NOTICE 'Successfully created flights for 30 days';
END $$;

-- =====================================================
-- BƯỚC 5: Verification - Kiểm tra kết quả
-- =====================================================

-- Đếm số FlightDefinitions
SELECT 
    'FlightDefinitions' as "Table",
    COUNT(*) as "Count"
FROM "FlightDefinitions";

-- Đếm số Flights được tạo
SELECT 
    'Flights' as "Table",
    COUNT(*) as "Count"
FROM "Flights"
WHERE "FlightDefinitionId" IS NOT NULL;

-- Đếm số FlightSeatInventories
SELECT 
    'FlightSeatInventories' as "Table",
    COUNT(*) as "Count"
FROM "FlightSeatInventories"
WHERE "FlightId" IN (
    SELECT "Id" FROM "Flights" WHERE "FlightDefinitionId" IS NOT NULL
);

-- Chi tiết flights theo ngày
SELECT 
    DATE(f."DepartureTime") as "Date",
    COUNT(*) as "TotalFlights"
FROM "Flights" f
WHERE f."FlightDefinitionId" IS NOT NULL
GROUP BY DATE(f."DepartureTime")
ORDER BY DATE(f."DepartureTime")
LIMIT 10;

-- Chi tiết flights theo FlightDefinition
SELECT 
    fd."FlightNumber",
    COUNT(f."Id") as "TotalFlights",
    MIN(f."DepartureTime") as "FirstFlight",
    MAX(f."DepartureTime") as "LastFlight"
FROM "FlightDefinitions" fd
LEFT JOIN "Flights" f ON f."FlightDefinitionId" = fd."Id"
GROUP BY fd."FlightNumber"
ORDER BY fd."FlightNumber";

-- Sample flights với seat inventory
SELECT 
    fd."FlightNumber",
    f."DepartureTime",
    f."ArrivalTime",
    sc."Name" as "SeatClass",
    fsi."TotalSeats",
    fsi."AvailableSeats",
    fsi."BasePrice"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "FlightSeatInventories" fsi ON fsi."FlightId" = f."Id"
INNER JOIN "SeatClasses" sc ON sc."Id" = fsi."SeatClassId"
WHERE DATE(f."DepartureTime") = CURRENT_DATE
ORDER BY f."DepartureTime", fd."FlightNumber", sc."Priority"
LIMIT 20;

-- =====================================================
-- HOÀN THÀNH!
-- =====================================================
-- Dữ liệu đã được tạo:
-- - FlightDefinitions: ~20 định nghĩa chuyến bay
-- - Flights: ~600 chuyến bay (20 definitions × 30 days)
-- - FlightSeatInventories: ~1800 records (600 flights × 3 seat classes)
-- =====================================================
