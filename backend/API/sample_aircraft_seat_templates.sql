-- Sample AircraftSeatTemplates Data
-- This creates seat configurations for each aircraft (only for existing aircraft 1-5)

-- Get SeatClass IDs
-- Assuming: 1=Economy, 2=Business, 3=First Class

-- Aircraft 1: Boeing 787-9 (Total: 296 seats)
INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted", "DeletedAt")
VALUES 
(1, 1, 246, 1500000, false, NULL),  -- Economy: 246 seats
(1, 2, 36, 4500000, false, NULL),   -- Business: 36 seats
(1, 3, 14, 8000000, false, NULL);   -- First: 14 seats

-- Aircraft 2: Airbus A321neo (Total: 220 seats)
INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted", "DeletedAt")
VALUES 
(2, 1, 196, 1200000, false, NULL),  -- Economy: 196 seats
(2, 2, 24, 3500000, false, NULL);   -- Business: 24 seats

-- Aircraft 3: Boeing 737 MAX 8 (Total: 189 seats)
INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted", "DeletedAt")
VALUES 
(3, 1, 165, 1000000, false, NULL),  -- Economy: 165 seats
(3, 2, 24, 3000000, false, NULL);   -- Business: 24 seats

-- Aircraft 4: Airbus A350-900 (Total: 325 seats)
INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted", "DeletedAt")
VALUES 
(4, 1, 261, 1800000, false, NULL),  -- Economy: 261 seats
(4, 2, 48, 5000000, false, NULL),   -- Business: 48 seats
(4, 3, 16, 9000000, false, NULL);   -- First: 16 seats

-- Aircraft 5: Boeing 777-300ER (Total: 364 seats)
INSERT INTO "AircraftSeatTemplates" ("AircraftId", "SeatClassId", "DefaultSeatCount", "DefaultBasePrice", "IsDeleted", "DeletedAt")
VALUES 
(5, 1, 296, 2000000, false, NULL),  -- Economy: 296 seats
(5, 2, 52, 5500000, false, NULL),   -- Business: 52 seats
(5, 3, 16, 10000000, false, NULL);  -- First: 16 seats

SELECT 'AircraftSeatTemplates inserted successfully!' as status;
SELECT COUNT(*) as total_templates FROM "AircraftSeatTemplates";
