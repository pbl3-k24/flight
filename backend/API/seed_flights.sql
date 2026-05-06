DO $$
DECLARE
    route_id INT;
    aircraft_id INT;
    eco_class_id INT;
    bus_class_id INT;
    new_flight_id INT;
    flight_date TIMESTAMP;
    departure_time TIMESTAMP;
    arrival_time TIMESTAMP;
    days_offset INT;
    random_suffix TEXT;
BEGIN
    SELECT "Id" INTO route_id FROM "Routes" LIMIT 1;
    SELECT "Id" INTO aircraft_id FROM "Aircraft" LIMIT 1;
    SELECT "Id" INTO eco_class_id FROM "SeatClasses" WHERE "Code" = 'ECO' LIMIT 1;
    SELECT "Id" INTO bus_class_id FROM "SeatClasses" WHERE "Code" = 'BUS' LIMIT 1;

    FOR days_offset IN 1..6 LOOP
        flight_date := CURRENT_DATE + (days_offset || ' days')::INTERVAL;
        random_suffix := (FLOOR(RANDOM() * 90000) + 10000)::TEXT;
        
        -- Flight 1 (08:00 UTC)
        departure_time := flight_date + interval '8 hours';
        arrival_time := departure_time + interval '2 hours';
        
        INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt", "Version", "IsDeleted")
        VALUES ('VN' || random_suffix || '1', route_id, aircraft_id, departure_time, arrival_time, 0, NOW(), NOW(), 0, false)
        RETURNING "Id" INTO new_flight_id;
        
        INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
        VALUES (new_flight_id, eco_class_id, 100, 100, 0, 0, 1500000, 1500000, 0, NOW(), NOW(), false);
        
        INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
        VALUES (new_flight_id, bus_class_id, 20, 20, 0, 0, 3000000, 3000000, 0, NOW(), NOW(), false);

        -- Flight 2 (14:00 UTC)
        departure_time := flight_date + interval '14 hours';
        arrival_time := departure_time + interval '2 hours';
        
        INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt", "Version", "IsDeleted")
        VALUES ('VN' || random_suffix || '2', route_id, aircraft_id, departure_time, arrival_time, 0, NOW(), NOW(), 0, false)
        RETURNING "Id" INTO new_flight_id;
        
        INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
        VALUES (new_flight_id, eco_class_id, 100, 100, 0, 0, 1200000, 1200000, 0, NOW(), NOW(), false);
        
        INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
        VALUES (new_flight_id, bus_class_id, 20, 20, 0, 0, 2800000, 2800000, 0, NOW(), NOW(), false);

    END LOOP;
END $$;
