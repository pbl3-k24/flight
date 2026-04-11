CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE aircrafts (
        id uuid NOT NULL,
        registration_code character varying(16) NOT NULL,
        model character varying(64) NOT NULL,
        total_seats integer NOT NULL,
        economy_seats integer NOT NULL,
        business_seats integer NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_aircrafts" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE airports (
        id uuid NOT NULL,
        iata_code character varying(8) NOT NULL,
        name character varying(256) NOT NULL,
        city character varying(128) NOT NULL,
        country character varying(64) NOT NULL DEFAULT 'Vietnam',
        time_zone character varying(64),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_airports" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        user_id uuid,
        action character varying(128) NOT NULL,
        entity_name character varying(128) NOT NULL,
        entity_id character varying(64),
        old_values jsonb,
        new_values jsonb,
        ip_address character varying(64),
        user_agent character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE email_templates (
        id uuid NOT NULL,
        template_key character varying(64) NOT NULL,
        subject character varying(256) NOT NULL,
        body_html text NOT NULL,
        body_text text,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_email_templates" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE fare_classes (
        id uuid NOT NULL,
        code character varying(32) NOT NULL,
        display_name character varying(64) NOT NULL,
        free_baggage_kg integer NOT NULL,
        meal_included boolean NOT NULL,
        refund_allowed boolean NOT NULL,
        refund_fee_percent numeric(5,2) NOT NULL,
        change_date_allowed boolean NOT NULL,
        change_date_fee_percent numeric(5,2) NOT NULL,
        description character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_fare_classes" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE idempotency_keys (
        id uuid NOT NULL,
        key character varying(256) NOT NULL,
        operation character varying(64) NOT NULL,
        response_body jsonb,
        response_status_code integer NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_idempotency_keys" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE notification_jobs (
        id uuid NOT NULL,
        job_type character varying(64) NOT NULL,
        status character varying(32) NOT NULL,
        recipient_email character varying(256) NOT NULL,
        template_key character varying(64) NOT NULL,
        payload jsonb NOT NULL,
        retry_count integer NOT NULL DEFAULT 0,
        max_retries integer NOT NULL DEFAULT 3,
        next_retry_at timestamp with time zone,
        sent_at timestamp with time zone,
        error_message character varying(2048),
        related_booking_id uuid,
        related_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_notification_jobs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE outbox_events (
        id uuid NOT NULL,
        event_type character varying(128) NOT NULL,
        payload jsonb NOT NULL,
        status character varying(32) NOT NULL,
        retry_count integer NOT NULL DEFAULT 0,
        processed_at timestamp with time zone,
        error_message character varying(2048),
        aggregate_id uuid,
        aggregate_name character varying(128),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_outbox_events" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        name character varying(64) NOT NULL,
        description character varying(256),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_roles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(256) NOT NULL,
        phone character varying(20),
        password_hash text,
        status character varying(32) NOT NULL,
        email_verified boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE wallet_ledger (
        id uuid NOT NULL,
        payment_id uuid,
        refund_id uuid,
        user_id uuid NOT NULL,
        entry_type character varying(16) NOT NULL,
        amount numeric(18,0) NOT NULL,
        currency character varying(8) NOT NULL DEFAULT 'VND',
        description character varying(512) NOT NULL,
        reference character varying(128),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_wallet_ledger" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE routes (
        id uuid NOT NULL,
        origin_airport_id uuid NOT NULL,
        destination_airport_id uuid NOT NULL,
        is_domestic boolean NOT NULL DEFAULT TRUE,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_routes" PRIMARY KEY (id),
        CONSTRAINT "FK_routes_airports_destination_airport_id" FOREIGN KEY (destination_airport_id) REFERENCES airports (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_routes_airports_origin_airport_id" FOREIGN KEY (origin_airport_id) REFERENCES airports (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE bookings (
        id uuid NOT NULL,
        booking_code character varying(32) NOT NULL,
        user_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        total_amount numeric(18,0) NOT NULL,
        currency character varying(8) NOT NULL DEFAULT 'VND',
        expires_at timestamp with time zone,
        contact_email character varying(256),
        contact_phone character varying(20),
        notes character varying(1024),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_bookings" PRIMARY KEY (id),
        CONSTRAINT "FK_bookings_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE oauth_accounts (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        provider character varying(32) NOT NULL,
        provider_user_id character varying(256) NOT NULL,
        access_token text,
        refresh_token text,
        token_expiry timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_oauth_accounts" PRIMARY KEY (id),
        CONSTRAINT "FK_oauth_accounts_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE user_profiles (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        full_name character varying(256) NOT NULL,
        date_of_birth date,
        gender character varying(16),
        nationality character varying(64),
        national_id character varying(32),
        passport_number character varying(32),
        passport_expiry date,
        avatar_url character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_user_profiles" PRIMARY KEY (id),
        CONSTRAINT "FK_user_profiles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE user_roles (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_user_roles" PRIMARY KEY (id),
        CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE flights (
        id uuid NOT NULL,
        flight_number character varying(16) NOT NULL,
        route_id uuid NOT NULL,
        aircraft_id uuid NOT NULL,
        departure_time timestamp with time zone NOT NULL,
        arrival_time timestamp with time zone NOT NULL,
        status character varying(32) NOT NULL,
        gate_number character varying(16),
        terminal character varying(16),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_flights" PRIMARY KEY (id),
        CONSTRAINT "FK_flights_aircrafts_aircraft_id" FOREIGN KEY (aircraft_id) REFERENCES aircrafts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_flights_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE price_rules (
        id uuid NOT NULL,
        route_id uuid,
        base_price numeric(18,0) NOT NULL,
        multiplier numeric(5,4) NOT NULL,
        day_of_week integer,
        season_month integer,
        days_before_departure integer,
        is_active boolean NOT NULL DEFAULT TRUE,
        priority integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_price_rules" PRIMARY KEY (id),
        CONSTRAINT "FK_price_rules_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE passengers (
        id uuid NOT NULL,
        booking_id uuid NOT NULL,
        full_name character varying(256) NOT NULL,
        date_of_birth date NOT NULL,
        gender character varying(16),
        national_id character varying(32),
        passport_number character varying(32),
        passport_expiry date,
        nationality character varying(64),
        passenger_type character varying(16) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_passengers" PRIMARY KEY (id),
        CONSTRAINT "FK_passengers_bookings_booking_id" FOREIGN KEY (booking_id) REFERENCES bookings (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE payments (
        id uuid NOT NULL,
        booking_id uuid NOT NULL,
        gateway character varying(32) NOT NULL,
        amount numeric(18,0) NOT NULL,
        currency character varying(8) NOT NULL DEFAULT 'VND',
        status character varying(32) NOT NULL,
        transaction_ref character varying(128),
        gateway_order_id character varying(128),
        idempotency_key character varying(128),
        paid_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_payments" PRIMARY KEY (id),
        CONSTRAINT "FK_payments_bookings_booking_id" FOREIGN KEY (booking_id) REFERENCES bookings (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE flight_fare_prices (
        id uuid NOT NULL,
        flight_id uuid NOT NULL,
        fare_class_id uuid NOT NULL,
        base_price numeric(18,0) NOT NULL,
        tax_amount numeric(18,0) NOT NULL,
        fee_amount numeric(18,0) NOT NULL,
        total_price numeric(18,0) NOT NULL,
        currency character varying(8) NOT NULL DEFAULT 'VND',
        source character varying(32) NOT NULL,
        effective_from timestamp with time zone NOT NULL,
        effective_to timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_flight_fare_prices" PRIMARY KEY (id),
        CONSTRAINT "FK_flight_fare_prices_fare_classes_fare_class_id" FOREIGN KEY (fare_class_id) REFERENCES fare_classes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_flight_fare_prices_flights_flight_id" FOREIGN KEY (flight_id) REFERENCES flights (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE flight_inventories (
        id uuid NOT NULL,
        flight_id uuid NOT NULL,
        fare_class_id uuid NOT NULL,
        total_seats integer NOT NULL,
        available_seats integer NOT NULL,
        held_seats integer NOT NULL DEFAULT 0,
        sold_seats integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_flight_inventories" PRIMARY KEY (id),
        CONSTRAINT "FK_flight_inventories_fare_classes_fare_class_id" FOREIGN KEY (fare_class_id) REFERENCES fare_classes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_flight_inventories_flights_flight_id" FOREIGN KEY (flight_id) REFERENCES flights (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE booking_items (
        id uuid NOT NULL,
        booking_id uuid NOT NULL,
        passenger_id uuid NOT NULL,
        flight_id uuid NOT NULL,
        fare_class_id uuid NOT NULL,
        seat_number character varying(8),
        unit_price numeric(18,0) NOT NULL,
        tax_amount numeric(18,0) NOT NULL,
        total_price numeric(18,0) NOT NULL,
        currency character varying(8) NOT NULL DEFAULT 'VND',
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_booking_items" PRIMARY KEY (id),
        CONSTRAINT "FK_booking_items_bookings_booking_id" FOREIGN KEY (booking_id) REFERENCES bookings (id) ON DELETE CASCADE,
        CONSTRAINT "FK_booking_items_fare_classes_fare_class_id" FOREIGN KEY (fare_class_id) REFERENCES fare_classes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_booking_items_flights_flight_id" FOREIGN KEY (flight_id) REFERENCES flights (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_booking_items_passengers_passenger_id" FOREIGN KEY (passenger_id) REFERENCES passengers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE payment_events (
        id uuid NOT NULL,
        payment_id uuid NOT NULL,
        event_type character varying(64) NOT NULL,
        raw_payload jsonb NOT NULL,
        gateway_signature character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_payment_events" PRIMARY KEY (id),
        CONSTRAINT "FK_payment_events_payments_payment_id" FOREIGN KEY (payment_id) REFERENCES payments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE refunds (
        id uuid NOT NULL,
        payment_id uuid NOT NULL,
        amount numeric(18,0) NOT NULL,
        reason character varying(512),
        status character varying(32) NOT NULL,
        gateway_refund_ref character varying(128),
        approved_by_user_id uuid,
        processed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_refunds" PRIMARY KEY (id),
        CONSTRAINT "FK_refunds_payments_payment_id" FOREIGN KEY (payment_id) REFERENCES payments (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE price_override_logs (
        id uuid NOT NULL,
        flight_fare_price_id uuid NOT NULL,
        admin_user_id uuid NOT NULL,
        price_before numeric(18,0) NOT NULL,
        price_after numeric(18,0) NOT NULL,
        reason character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_price_override_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_price_override_logs_flight_fare_prices_flight_fare_price_id" FOREIGN KEY (flight_fare_price_id) REFERENCES flight_fare_prices (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE TABLE tickets (
        id uuid NOT NULL,
        ticket_number character varying(64) NOT NULL,
        booking_item_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        issued_at timestamp with time zone NOT NULL,
        barcode_data character varying(512),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_tickets" PRIMARY KEY (id),
        CONSTRAINT "FK_tickets_booking_items_booking_item_id" FOREIGN KEY (booking_item_id) REFERENCES booking_items (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000001', 'Hồ Chí Minh', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828514Z', NULL, 'SGN', 'Sân bay Quốc tế Tân Sơn Nhất', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828514Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000002', 'Hà Nội', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828725Z', NULL, 'HAN', 'Sân bay Quốc tế Nội Bài', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828725Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000003', 'Đà Nẵng', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828727Z', NULL, 'DAD', 'Sân bay Quốc tế Đà Nẵng', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828727Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000004', 'Nha Trang', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828729Z', NULL, 'CXR', 'Sân bay Quốc tế Cam Ranh', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828729Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000005', 'Phú Quốc', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.82873Z', NULL, 'PQC', 'Sân bay Quốc tế Phú Quốc', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.82873Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000006', 'Hải Phòng', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828752Z', NULL, 'HPH', 'Sân bay Quốc tế Cát Bi', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828752Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000007', 'Huế', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828754Z', NULL, 'HUI', 'Sân bay Quốc tế Phú Bài', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828754Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000008', 'Quy Nhơn', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828755Z', NULL, 'UIH', 'Sân bay Phù Cát', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828755Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000009', 'Buôn Ma Thuột', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828756Z', NULL, 'BMV', 'Sân bay Buôn Ma Thuột', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828756Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000010', 'Đà Lạt', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828758Z', NULL, 'DLI', 'Sân bay Quốc tế Liên Khương', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828758Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000011', 'Tam Kỳ', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828759Z', NULL, 'VCL', 'Sân bay Chu Lai', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828759Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000012', 'Đồng Hới', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828761Z', NULL, 'VDH', 'Sân bay Đồng Hới', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828761Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000013', 'Vinh', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828762Z', NULL, 'VII', 'Sân bay Vinh', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828762Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000014', 'Cần Thơ', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828763Z', NULL, 'VCA', 'Sân bay Quốc tế Cần Thơ', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828763Z');
    INSERT INTO airports (id, city, country, created_at, deleted_at, iata_code, name, time_zone, updated_at)
    VALUES ('10000000-0000-0000-0000-000000000015', 'Rạch Giá', 'Vietnam', TIMESTAMPTZ '2026-04-11T08:49:14.828765Z', NULL, 'VKG', 'Sân bay Rạch Giá', 'Asia/Ho_Chi_Minh', TIMESTAMPTZ '2026-04-11T08:49:14.828765Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    INSERT INTO fare_classes (id, change_date_allowed, change_date_fee_percent, code, created_at, deleted_at, description, display_name, free_baggage_kg, meal_included, refund_allowed, refund_fee_percent, updated_at)
    VALUES ('20000000-0000-0000-0000-000000000001', TRUE, 15.0, 'Economy', TIMESTAMPTZ '2026-04-11T08:49:14.833938Z', NULL, 'Hạng phổ thông tiêu chuẩn', 'Phổ thông', 23, FALSE, TRUE, 30.0, TIMESTAMPTZ '2026-04-11T08:49:14.833938Z');
    INSERT INTO fare_classes (id, change_date_allowed, change_date_fee_percent, code, created_at, deleted_at, description, display_name, free_baggage_kg, meal_included, refund_allowed, refund_fee_percent, updated_at)
    VALUES ('20000000-0000-0000-0000-000000000002', TRUE, 5.0, 'Business', TIMESTAMPTZ '2026-04-11T08:49:14.83439Z', NULL, 'Hạng thương gia cao cấp', 'Thương gia', 40, TRUE, TRUE, 10.0, TIMESTAMPTZ '2026-04-11T08:49:14.83439Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    INSERT INTO roles (id, created_at, deleted_at, description, name, updated_at)
    VALUES ('00000000-0000-0000-0000-000000000001', TIMESTAMPTZ '2026-04-11T08:49:14.913006Z', NULL, 'System administrator', 'Admin', TIMESTAMPTZ '2026-04-11T08:49:14.913006Z');
    INSERT INTO roles (id, created_at, deleted_at, description, name, updated_at)
    VALUES ('00000000-0000-0000-0000-000000000002', TIMESTAMPTZ '2026-04-11T08:49:14.913111Z', NULL, 'Airline staff', 'Staff', TIMESTAMPTZ '2026-04-11T08:49:14.913111Z');
    INSERT INTO roles (id, created_at, deleted_at, description, name, updated_at)
    VALUES ('00000000-0000-0000-0000-000000000003', TIMESTAMPTZ '2026-04-11T08:49:14.913112Z', NULL, 'Regular customer', 'Customer', TIMESTAMPTZ '2026-04-11T08:49:14.913113Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_aircrafts_registration_code ON aircrafts (registration_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_airports_city ON airports (city);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_airports_iata_code ON airports (iata_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_audit_logs_created_at ON audit_logs (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_name, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_audit_logs_user_id ON audit_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_booking_items_booking_id ON booking_items (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_booking_items_fare_class_id" ON booking_items (fare_class_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_booking_items_flight_id ON booking_items (flight_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_booking_items_passenger_id ON booking_items (passenger_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_bookings_booking_code ON bookings (booking_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_bookings_expires_at ON bookings (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_bookings_status ON bookings (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_bookings_user_id ON bookings (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_email_templates_key ON email_templates (template_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_fare_classes_code ON fare_classes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_flight_fare_prices_fare_class_id" ON flight_fare_prices (fare_class_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flight_fare_prices_flight_fare_date ON flight_fare_prices (flight_id, fare_class_id, effective_from);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flight_fare_prices_flight_id ON flight_fare_prices (flight_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_flight_inventories_fare_class_id" ON flight_inventories (fare_class_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_flight_inventories_flight_fare ON flight_inventories (flight_id, fare_class_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flight_inventories_flight_id ON flight_inventories (flight_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_flights_aircraft_id" ON flights (aircraft_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flights_departure_time ON flights (departure_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flights_flight_number ON flights (flight_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flights_is_deleted ON flights (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flights_route_departure ON flights (route_id, departure_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_flights_status ON flights (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_idempotency_keys_expires_at ON idempotency_keys (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_idempotency_keys_key ON idempotency_keys (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_notification_jobs_booking_id ON notification_jobs (related_booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_notification_jobs_next_retry_at ON notification_jobs (next_retry_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_notification_jobs_status ON notification_jobs (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_oauth_accounts_provider_uid ON oauth_accounts (provider, provider_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_oauth_accounts_user_id ON oauth_accounts (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_outbox_events_aggregate ON outbox_events (aggregate_id, aggregate_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_outbox_events_created_at ON outbox_events (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_outbox_events_status ON outbox_events (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_passengers_booking_id ON passengers (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_payment_events_payment_id ON payment_events (payment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_payments_booking_id ON payments (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_payments_idempotency_key ON payments (idempotency_key) WHERE idempotency_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_payments_status ON payments (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_payments_transaction_ref ON payments (transaction_ref);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_price_override_logs_admin_user_id ON price_override_logs (admin_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_price_override_logs_fare_price_id ON price_override_logs (flight_fare_price_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_price_rules_route_active_priority ON price_rules (route_id, is_active, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_refunds_payment_id ON refunds (payment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_refunds_status ON refunds (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_roles_name ON roles (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_routes_destination_airport_id" ON routes (destination_airport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_routes_is_active ON routes (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_routes_origin_destination ON routes (origin_airport_id, destination_airport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_tickets_booking_item_id ON tickets (booking_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_tickets_status ON tickets (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_tickets_ticket_number ON tickets (ticket_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_user_profiles_user_id ON user_profiles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX "IX_user_roles_role_id" ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_user_roles_user_role ON user_roles (user_id, role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_users_is_deleted ON users (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_users_phone ON users (phone);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_users_status ON users (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_wallet_ledger_created_at ON wallet_ledger (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_wallet_ledger_payment_id ON wallet_ledger (payment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    CREATE INDEX ix_wallet_ledger_user_id ON wallet_ledger (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260411084916_InitialSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260411084916_InitialSchema', '9.0.4');
    END IF;
END $EF$;
COMMIT;

