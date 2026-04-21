-- =============================================================
-- SAMPLE DATA FOR FLIGHT BOOKING SYSTEM - TEST SEARCH FEATURES
-- =============================================================

-- ============= AIRPORTS =============
-- Các sân bay Việt Nam chính
INSERT INTO "Airports" ("Code", "Name", "City", "Province", "IsActive") VALUES
-- Hiện có
('SGN', 'Sân bay Tân Sơn Nhất', 'Thành phố Hồ Chí Minh', 'Hồ Chí Minh', true),
('HAN', 'Sân bay Nội Bài', 'Hà Nội', 'Hà Nội', true),
('DAD', 'Sân bay Quốc tế Đà Nẵng', 'Đà Nẵng', 'Đà Nẵng', true),
('CTS', 'Sân bay Cần Thơ', 'Cần Thơ', 'Cần Thơ', true),
-- Thêm mới
('VCA', 'Sân bay Buôn Mê Thuộc', 'Buôn Mê Thuộc', 'Đắk Lắk', true),
('UIH', 'Sân bay Phù Cát', 'Tuy Hòa', 'Phú Yên', true),
('HUI', 'Sân bay Phú Bài', 'Huế', 'Thừa Thiên Huế', true),
('QNH', 'Sân bay Kỳ Anh', 'Hà Tĩnh', 'Hà Tĩnh', true),
('VDH', 'Sân bay Điện Biên Phủ', 'Điện Biên Phủ', 'Điện Biên', true),
('HAN2', 'Sân bay Nội Bài Backup', 'Hà Nội', 'Hà Nội', false);

-- ============= ROUTES =============
-- Tuyến bay nội địa chính
INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 1700, 145, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'SGN' AND a2."Code" = 'HAN'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 1700, 145, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'HAN' AND a2."Code" = 'SGN'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 960, 100, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'SGN' AND a2."Code" = 'DAD'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 960, 100, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'DAD' AND a2."Code" = 'SGN'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 500, 75, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'HAN' AND a2."Code" = 'DAD'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 330, 55, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'SGN' AND a2."Code" = 'CTS'

INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive") 
SELECT a1.Id, a2.Id, 500, 70, true 
FROM "Airports" a1, "Airports" a2 WHERE a1."Code" = 'HAN' AND a2."Code" = 'HUI'

-- ============= AIRCRAFT =============
INSERT INTO "Aircraft" ("Model", "RegistrationNumber", "TotalSeats", "IsActive") VALUES
('Boeing 737', 'VN-ABC123', 180, true),
('Airbus A320', 'VN-XYZ789', 220, true),
('Boeing 787', 'VN-DEF456', 242, true),
('Airbus A321', 'VN-GHI789', 236, true),
('Boeing 777', 'VN-JKL012', 350, true),
('Airbus A330', 'VN-MNO345', 295, false);

-- ============= SEAT CLASSES =============
INSERT INTO "SeatClasses" ("Code", "Name", "RefundPercent", "ChangeFee", "Priority") VALUES
('ECO', 'Economy', 100, 150000, 3),
('BUS', 'Business', 80, 200000, 2),
('PRM', 'Premium', 60, 300000, 1);

-- ============= FLIGHTS =============
-- Các chuyến bay trong các ngày tới
-- Chuyến bay chiều sáng: HCM -> HN
INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt")
VALUES 
('VN001', (SELECT id FROM "Routes" WHERE "DepartureAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'SGN') AND "ArrivalAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'HAN') LIMIT 1), 1, CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '8 hours', CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '9 hours' + INTERVAL '45 minutes', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Chuyến bay chiều trưa: HN -> HCM
INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt")
VALUES 
('VN002', (SELECT id FROM "Routes" WHERE "DepartureAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'HAN') AND "ArrivalAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'SGN') LIMIT 1), 2, CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '12 hours', CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '13 hours' + INTERVAL '45 minutes', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Chuyến bay chiều tối: HCM -> Đà Nẵng
INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt")
VALUES 
('VN003', (SELECT id FROM "Routes" WHERE "DepartureAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'SGN') AND "ArrivalAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'DAD') LIMIT 1), 3, CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '17 hours', CURRENT_TIMESTAMP + INTERVAL '1 day' + INTERVAL '18 hours' + INTERVAL '40 minutes', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Chuyến bay hôm sau: Đà Nẵng -> HCM
INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt")
VALUES 
('VN004', (SELECT id FROM "Routes" WHERE "DepartureAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'DAD') AND "ArrivalAirportId" = (SELECT id FROM "Airports" WHERE "Code" = 'SGN') LIMIT 1), 2, CURRENT_TIMESTAMP + INTERVAL '2 days' + INTERVAL '7 hours', CURRENT_TIMESTAMP + INTERVAL '2 days' + INTERVAL '8 hours' + INTERVAL '40 minutes', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ============= FLIGHT SEAT INVENTORIES =============
-- Cho chuyến VN001
INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "SoldSeats", "HeldSeats", "BasePrice", "CurrentPrice", "Version")
VALUES 
-- Economy: 150 ghế
(1, 1, 150, 120, 20, 10, 1500000, 1650000, 0),
-- Business: 30 ghế
(1, 2, 30, 25, 5, 0, 3000000, 3300000, 0);

-- Cho chuyến VN002
INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "SoldSeats", "HeldSeats", "BasePrice", "CurrentPrice", "Version")
VALUES 
(2, 1, 180, 150, 25, 5, 1400000, 1540000, 0),
(2, 2, 40, 35, 5, 0, 2800000, 3080000, 0);

-- Cho chuyến VN003
INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "SoldSeats", "HeldSeats", "BasePrice", "CurrentPrice", "Version")
VALUES 
(3, 1, 200, 180, 15, 5, 1200000, 1320000, 0),
(3, 2, 42, 40, 2, 0, 2400000, 2640000, 0),
(3, 3, 10, 8, 2, 0, 3600000, 3960000, 0);

-- Cho chuyến VN004
INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "SoldSeats", "HeldSeats", "BasePrice", "CurrentPrice", "Version")
VALUES 
(4, 1, 180, 160, 15, 5, 1400000, 1540000, 0),
(4, 2, 40, 38, 2, 0, 2800000, 3080000, 0);

-- ============= PROMOTIONS =============
INSERT INTO "Promotions" ("Code", "DiscountType", "DiscountValue", "ValidFrom", "ValidTo", "UsageLimit", "UsedCount", "IsActive", "CreatedAt")
VALUES
-- Mã 20% discount
('SUMMER20', 0, 20, CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP + INTERVAL '90 days', 500, 45, true, CURRENT_TIMESTAMP),
-- Mã 500k discount cố định
('SAVE500K', 1, 500000, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP + INTERVAL '30 days', 300, 120, true, CURRENT_TIMESTAMP),
-- Mã 15% discount cho early bird
('EARLYBIRD15', 0, 15, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '60 days', 200, 80, true, CURRENT_TIMESTAMP),
-- Mã hết hạn
('EXPIRED2024', 0, 25, CURRENT_TIMESTAMP - INTERVAL '100 days', CURRENT_TIMESTAMP - INTERVAL '10 days', 100, 100, false, CURRENT_TIMESTAMP - INTERVAL '100 days'),
-- Mã 10% với giới hạn đã full
('VIP10', 0, 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '45 days', 50, 50, true, CURRENT_TIMESTAMP),
-- Mã mới
('NEWYEAR2025', 0, 30, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '120 days', 1000, 5, true, CURRENT_TIMESTAMP);

-- ============= BOOKINGS =============
INSERT INTO "Bookings" ("UserId", "OutboundFlightId", "BookingCode", "Status", "ContactEmail", "TotalAmount", "FinalAmount", "CreatedAt", "UpdatedAt", "ExpiresAt")
VALUES
(2, 1, 'BK' || LPAD(FLOOR(RANDOM() * 1000000)::text, 8, '0'), 0, 'user1@gmail.com', 1650000, 1485000, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '10 days'),
(3, 2, 'BK' || LPAD(FLOOR(RANDOM() * 1000000)::text, 8, '0'), 1, 'user2@gmail.com', 1540000, 1386000, CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '8 days');
