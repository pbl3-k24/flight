DO $$
DECLARE
    sgn_id INT;
    han_id INT;
    dad_id INT;
    cts_id INT;
    vca_id INT;
    hui_id INT;

    route_sgn_han INT;
    route_han_sgn INT;
    route_sgn_dad INT;
    route_dad_sgn INT;
    route_han_dad INT;
    route_sgn_cts INT;
    route_cts_sgn INT;
    route_han_hui INT;

    aircraft1_id INT;
    aircraft2_id INT;
    aircraft3_id INT;
    aircraft4_id INT;
    aircraft5_id INT;

    eco_id INT;
    bus_id INT;
    prm_id INT;

    start_date TIMESTAMP;
    end_date TIMESTAMP := '2026-05-05 00:00:00';
    flight_date TIMESTAMP;
    departure_time TIMESTAMP;
    arrival_time TIMESTAMP;
    
    r_id INT;
    route_array INT[];
    duration_array INT[];
    r_idx INT;
    
    ac_id INT;
    ac_array INT[];
    
    flight_counter INT := 1;
    new_flight_id INT;
    random_minutes INT;
    hour_val INT;
BEGIN
    -- 1. Insert Airports if not exist
    INSERT INTO "Airports" ("Code", "Name", "City", "Province", "IsActive", "IsDeleted") VALUES
    ('SGN', 'Sân bay Tân Sơn Nhất', 'Thành phố Hồ Chí Minh', 'Hồ Chí Minh', true, false),
    ('HAN', 'Sân bay Nội Bài', 'Hà Nội', 'Hà Nội', true, false),
    ('DAD', 'Sân bay Quốc tế Đà Nẵng', 'Đà Nẵng', 'Đà Nẵng', true, false),
    ('CTS', 'Sân bay Cần Thơ', 'Cần Thơ', 'Cần Thơ', true, false),
    ('VCA', 'Sân bay Buôn Mê Thuột', 'Buôn Mê Thuột', 'Đắk Lắk', true, false),
    ('HUI', 'Sân bay Phú Bài', 'Huế', 'Thừa Thiên Huế', true, false)
    ON CONFLICT DO NOTHING;

    SELECT "Id" INTO sgn_id FROM "Airports" WHERE "Code" = 'SGN' LIMIT 1;
    SELECT "Id" INTO han_id FROM "Airports" WHERE "Code" = 'HAN' LIMIT 1;
    SELECT "Id" INTO dad_id FROM "Airports" WHERE "Code" = 'DAD' LIMIT 1;
    SELECT "Id" INTO cts_id FROM "Airports" WHERE "Code" = 'CTS' LIMIT 1;
    SELECT "Id" INTO vca_id FROM "Airports" WHERE "Code" = 'VCA' LIMIT 1;
    SELECT "Id" INTO hui_id FROM "Airports" WHERE "Code" = 'HUI' LIMIT 1;

    -- 2. Insert Routes
    INSERT INTO "Routes" ("DepartureAirportId", "ArrivalAirportId", "DistanceKm", "EstimatedDurationMinutes", "IsActive", "IsDeleted") VALUES
    (sgn_id, han_id, 1700, 145, true, false),
    (han_id, sgn_id, 1700, 145, true, false),
    (sgn_id, dad_id, 960, 100, true, false),
    (dad_id, sgn_id, 960, 100, true, false),
    (han_id, dad_id, 500, 75, true, false),
    (sgn_id, cts_id, 330, 55, true, false),
    (cts_id, sgn_id, 330, 55, true, false),
    (han_id, hui_id, 650, 85, true, false);

    SELECT "Id" INTO route_sgn_han FROM "Routes" WHERE "DepartureAirportId" = sgn_id AND "ArrivalAirportId" = han_id LIMIT 1;
    SELECT "Id" INTO route_han_sgn FROM "Routes" WHERE "DepartureAirportId" = han_id AND "ArrivalAirportId" = sgn_id LIMIT 1;
    SELECT "Id" INTO route_sgn_dad FROM "Routes" WHERE "DepartureAirportId" = sgn_id AND "ArrivalAirportId" = dad_id LIMIT 1;
    SELECT "Id" INTO route_dad_sgn FROM "Routes" WHERE "DepartureAirportId" = dad_id AND "ArrivalAirportId" = sgn_id LIMIT 1;
    SELECT "Id" INTO route_han_dad FROM "Routes" WHERE "DepartureAirportId" = han_id AND "ArrivalAirportId" = dad_id LIMIT 1;
    SELECT "Id" INTO route_sgn_cts FROM "Routes" WHERE "DepartureAirportId" = sgn_id AND "ArrivalAirportId" = cts_id LIMIT 1;
    SELECT "Id" INTO route_cts_sgn FROM "Routes" WHERE "DepartureAirportId" = cts_id AND "ArrivalAirportId" = sgn_id LIMIT 1;
    SELECT "Id" INTO route_han_hui FROM "Routes" WHERE "DepartureAirportId" = han_id AND "ArrivalAirportId" = hui_id LIMIT 1;

    route_array := ARRAY[route_sgn_han, route_han_sgn, route_sgn_dad, route_dad_sgn, route_han_dad, route_sgn_cts, route_cts_sgn, route_han_hui];
    duration_array := ARRAY[145, 145, 100, 100, 75, 55, 55, 85];

    -- 3. Insert Aircraft
    INSERT INTO "Aircraft" ("Model", "RegistrationNumber", "TotalSeats", "IsActive", "IsDeleted") VALUES
    ('Boeing 737', 'VN-ABC123', 180, true, false),
    ('Airbus A320', 'VN-XYZ789', 220, true, false),
    ('Boeing 787', 'VN-DEF456', 242, true, false),
    ('Airbus A321', 'VN-GHI789', 236, true, false),
    ('Boeing 777', 'VN-JKL012', 350, true, false);
    
    SELECT array_agg("Id") INTO ac_array FROM "Aircraft";

    -- 4. Insert Seat Classes
    INSERT INTO "SeatClasses" ("Code", "Name", "RefundPercent", "ChangeFee", "Priority", "IsDeleted") VALUES
    ('ECO', 'Economy', 100, 150000, 3, false),
    ('BUS', 'Business', 80, 200000, 2, false),
    ('PRM', 'Premium', 60, 300000, 1, false)
    ON CONFLICT DO NOTHING;
    
    SELECT "Id" INTO eco_id FROM "SeatClasses" WHERE "Code" = 'ECO' LIMIT 1;
    SELECT "Id" INTO bus_id FROM "SeatClasses" WHERE "Code" = 'BUS' LIMIT 1;
    SELECT "Id" INTO prm_id FROM "SeatClasses" WHERE "Code" = 'PRM' LIMIT 1;

    -- 5. Insert Flights
    start_date := CURRENT_DATE;
    
    FOR flight_date IN SELECT generate_series(start_date, end_date, '1 day'::interval) LOOP
        -- 4 times per day: 6h, 10h, 14h, 18h
        FOREACH hour_val IN ARRAY ARRAY[6, 10, 14, 18] LOOP
            FOR r_idx IN 1..8 LOOP
                r_id := route_array[r_idx];
                ac_id := ac_array[1 + (flight_counter % 5)];
                random_minutes := FLOOR(RANDOM() * 30);
                
                departure_time := flight_date + (hour_val || ' hours')::interval + (random_minutes || ' minutes')::interval;
                
                -- Ensure future time
                IF departure_time < NOW() + interval '2 hours' THEN
                    departure_time := NOW() + interval '2 hours' + (random_minutes || ' minutes')::interval;
                END IF;
                
                arrival_time := departure_time + (duration_array[r_idx] || ' minutes')::interval;
                
                INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt", "Version", "IsDeleted")
                VALUES ('VN' || (EXTRACT(DAY FROM flight_date) * 1000 + flight_counter)::TEXT, r_id, ac_id, departure_time, arrival_time, 0, NOW(), NOW(), 0, false)
                RETURNING "Id" INTO new_flight_id;
                
                -- Inventories
                INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
                VALUES (new_flight_id, eco_id, 150, 150, 0, 0, 1200000, 1200000, 0, NOW(), NOW(), false);
                
                INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
                VALUES (new_flight_id, bus_id, 30, 30, 0, 0, 3000000, 3000000, 0, NOW(), NOW(), false);
                
                INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
                VALUES (new_flight_id, prm_id, 15, 15, 0, 0, 4500000, 4500000, 0, NOW(), NOW(), false);
                
                flight_counter := flight_counter + 1;
            END LOOP;
        END LOOP;
    END LOOP;

END $$;
