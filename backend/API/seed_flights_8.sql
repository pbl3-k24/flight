DO $$
DECLARE
    aircraft_id INT;
    eco_class_id INT;
    bus_class_id INT;
    prm_class_id INT;
    new_flight_id INT;
    flight_date TIMESTAMP;
    departure_time TIMESTAMP;
    arrival_time TIMESTAMP;
    days_offset INT;
    hour_offset INT;
    random_suffix TEXT;
    routes_array INT[];
    route_count INT;
    r_id INT;
BEGIN
    SELECT "Id" INTO aircraft_id FROM "Aircraft" LIMIT 1;
    SELECT "Id" INTO eco_class_id FROM "SeatClasses" WHERE "Code" = 'ECO' LIMIT 1;
    SELECT "Id" INTO bus_class_id FROM "SeatClasses" WHERE "Code" = 'BUS' LIMIT 1;
    SELECT "Id" INTO prm_class_id FROM "SeatClasses" WHERE "Code" = 'PRM' LIMIT 1;

    -- Load all route IDs into an array
    SELECT array_agg("Id") INTO routes_array FROM "Routes";
    route_count := array_length(routes_array, 1);

    FOR days_offset IN 1..6 LOOP
        flight_date := CURRENT_DATE + (days_offset || ' days')::INTERVAL;
        
        -- 8 flights per day
        FOR hour_offset IN 1..8 LOOP
            -- Pick a route using modulus (this ensures variation across all available routes)
            r_id := routes_array[1 + (hour_offset % route_count)];

            random_suffix := (FLOOR(RANDOM() * 90000) + 10000)::TEXT;
            
            -- Departure time starts from 6:00 UTC, every 2 hours
            departure_time := flight_date + ((4 + hour_offset * 2) || ' hours')::INTERVAL;
            arrival_time := departure_time + interval '2 hours';
            
            INSERT INTO "Flights" ("FlightNumber", "RouteId", "AircraftId", "DepartureTime", "ArrivalTime", "Status", "CreatedAt", "UpdatedAt", "Version", "IsDeleted")
            VALUES ('VN' || random_suffix || hour_offset::TEXT, r_id, aircraft_id, departure_time, arrival_time, 0, NOW(), NOW(), 0, false)
            RETURNING "Id" INTO new_flight_id;
            
            INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
            VALUES (new_flight_id, eco_class_id, 150, 150, 0, 0, 1500000, 1500000, 0, NOW(), NOW(), false);
            
            INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
            VALUES (new_flight_id, bus_class_id, 30, 30, 0, 0, 3000000, 3000000, 0, NOW(), NOW(), false);
            
            IF prm_class_id IS NOT NULL THEN
                INSERT INTO "FlightSeatInventories" ("FlightId", "SeatClassId", "TotalSeats", "AvailableSeats", "HeldSeats", "SoldSeats", "BasePrice", "CurrentPrice", "Version", "CreatedAt", "UpdatedAt", "IsDeleted")
                VALUES (new_flight_id, prm_class_id, 15, 15, 0, 0, 4500000, 4500000, 0, NOW(), NOW(), false);
            END IF;
            
        END LOOP;
    END LOOP;
END $$;
