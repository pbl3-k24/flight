using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aircrafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    economy_seats = table.Column<int>(type: "integer", nullable: false),
                    business_seats = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aircrafts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    iata_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Vietnam"),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fare_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    free_baggage_kg = table.Column<int>(type: "integer", nullable: false),
                    meal_included = table.Column<bool>(type: "boolean", nullable: false),
                    refund_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    refund_fee_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    change_date_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    change_date_fee_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fare_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    response_status_code = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipient_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    template_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    related_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aggregate_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_ledger", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_domestic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                    table.ForeignKey(
                        name: "FK_routes_airports_destination_airport_id",
                        column: x => x.destination_airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_routes_airports_origin_airport_id",
                        column: x => x.origin_airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "oauth_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_oauth_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    nationality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    national_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    passport_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    passport_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aircraft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gate_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    terminal = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flights", x => x.id);
                    table.ForeignKey(
                        name: "FK_flights_aircrafts_aircraft_id",
                        column: x => x.aircraft_id,
                        principalTable: "aircrafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flights_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    season_month = table.Column<int>(type: "integer", nullable: true),
                    days_before_departure = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_rules_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    national_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    passport_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    passport_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    passenger_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passengers", x => x.id);
                    table.ForeignKey(
                        name: "FK_passengers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    transaction_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    gateway_order_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flight_fare_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fare_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_fare_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_flight_fare_prices_fare_classes_fare_class_id",
                        column: x => x.fare_class_id,
                        principalTable: "fare_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_fare_prices_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flight_inventories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fare_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    available_seats = table.Column<int>(type: "integer", nullable: false),
                    held_seats = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sold_seats = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_inventories", x => x.id);
                    table.ForeignKey(
                        name: "FK_flight_inventories_fare_classes_fare_class_id",
                        column: x => x.fare_class_id,
                        principalTable: "fare_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_inventories_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fare_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_number = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_items_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_items_fare_classes_fare_class_id",
                        column: x => x.fare_class_id,
                        principalTable: "fare_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_items_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_items_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: false),
                    gateway_signature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_events_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gateway_refund_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.id);
                    table.ForeignKey(
                        name: "FK_refunds_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_override_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_fare_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_before = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    price_after = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_override_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_override_logs_flight_fare_prices_flight_fare_price_id",
                        column: x => x.flight_fare_price_id,
                        principalTable: "flight_fare_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    booking_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    barcode_data = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                    table.ForeignKey(
                        name: "FK_tickets_booking_items_booking_item_id",
                        column: x => x.booking_item_id,
                        principalTable: "booking_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "airports",
                columns: new[] { "id", "city", "country", "created_at", "deleted_at", "iata_code", "name", "time_zone", "updated_at" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Hồ Chí Minh", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(5146), null, "SGN", "Sân bay Quốc tế Tân Sơn Nhất", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(5146) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Hà Nội", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7256), null, "HAN", "Sân bay Quốc tế Nội Bài", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7257) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Đà Nẵng", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7276), null, "DAD", "Sân bay Quốc tế Đà Nẵng", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7276) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Nha Trang", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7290), null, "CXR", "Sân bay Quốc tế Cam Ranh", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7291) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Phú Quốc", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7305), null, "PQC", "Sân bay Quốc tế Phú Quốc", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7305) },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Hải Phòng", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7523), null, "HPH", "Sân bay Quốc tế Cát Bi", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7523) },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Huế", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7540), null, "HUI", "Sân bay Quốc tế Phú Bài", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7541) },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Quy Nhơn", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7555), null, "UIH", "Sân bay Phù Cát", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7555) },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Buôn Ma Thuột", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7568), null, "BMV", "Sân bay Buôn Ma Thuột", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7569) },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "Đà Lạt", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7582), null, "DLI", "Sân bay Quốc tế Liên Khương", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7583) },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Tam Kỳ", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7597), null, "VCL", "Sân bay Chu Lai", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7597) },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "Đồng Hới", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7610), null, "VDH", "Sân bay Đồng Hới", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7611) },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Vinh", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7625), null, "VII", "Sân bay Vinh", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7625) },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "Cần Thơ", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7639), null, "VCA", "Sân bay Quốc tế Cần Thơ", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7639) },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "Rạch Giá", "Vietnam", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7652), null, "VKG", "Sân bay Rạch Giá", "Asia/Ho_Chi_Minh", new DateTime(2026, 4, 11, 8, 49, 14, 828, DateTimeKind.Utc).AddTicks(7653) }
                });

            migrationBuilder.InsertData(
                table: "fare_classes",
                columns: new[] { "id", "change_date_allowed", "change_date_fee_percent", "code", "created_at", "deleted_at", "description", "display_name", "free_baggage_kg", "meal_included", "refund_allowed", "refund_fee_percent", "updated_at" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), true, 15m, "Economy", new DateTime(2026, 4, 11, 8, 49, 14, 833, DateTimeKind.Utc).AddTicks(9388), null, "Hạng phổ thông tiêu chuẩn", "Phổ thông", 23, false, true, 30m, new DateTime(2026, 4, 11, 8, 49, 14, 833, DateTimeKind.Utc).AddTicks(9389) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), true, 5m, "Business", new DateTime(2026, 4, 11, 8, 49, 14, 834, DateTimeKind.Utc).AddTicks(3908), null, "Hạng thương gia cao cấp", "Thương gia", 40, true, true, 10m, new DateTime(2026, 4, 11, 8, 49, 14, 834, DateTimeKind.Utc).AddTicks(3909) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "deleted_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(68), null, "System administrator", "Admin", new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(69) },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(1110), null, "Airline staff", "Staff", new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(1110) },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(1129), null, "Regular customer", "Customer", new DateTime(2026, 4, 11, 8, 49, 14, 913, DateTimeKind.Utc).AddTicks(1130) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_aircrafts_registration_code",
                table: "aircrafts",
                column: "registration_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_airports_city",
                table: "airports",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "ix_airports_iata_code",
                table: "airports",
                column: "iata_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_items_booking_id",
                table: "booking_items",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_fare_class_id",
                table: "booking_items",
                column: "fare_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_items_flight_id",
                table: "booking_items",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_items_passenger_id",
                table: "booking_items",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_booking_code",
                table: "bookings",
                column: "booking_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_expires_at",
                table: "bookings",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user_id",
                table: "bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_templates_key",
                table: "email_templates",
                column: "template_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fare_classes_code",
                table: "fare_classes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_flight_fare_prices_fare_class_id",
                table: "flight_fare_prices",
                column: "fare_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_flight_fare_prices_flight_fare_date",
                table: "flight_fare_prices",
                columns: new[] { "flight_id", "fare_class_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_flight_fare_prices_flight_id",
                table: "flight_fare_prices",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_flight_inventories_fare_class_id",
                table: "flight_inventories",
                column: "fare_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_flight_inventories_flight_fare",
                table: "flight_inventories",
                columns: new[] { "flight_id", "fare_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_flight_inventories_flight_id",
                table: "flight_inventories",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_flights_aircraft_id",
                table: "flights",
                column: "aircraft_id");

            migrationBuilder.CreateIndex(
                name: "ix_flights_departure_time",
                table: "flights",
                column: "departure_time");

            migrationBuilder.CreateIndex(
                name: "ix_flights_flight_number",
                table: "flights",
                column: "flight_number");

            migrationBuilder.CreateIndex(
                name: "ix_flights_is_deleted",
                table: "flights",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_flights_route_departure",
                table: "flights",
                columns: new[] { "route_id", "departure_time" });

            migrationBuilder.CreateIndex(
                name: "ix_flights_status",
                table: "flights",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_key",
                table: "idempotency_keys",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_jobs_booking_id",
                table: "notification_jobs",
                column: "related_booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_jobs_next_retry_at",
                table: "notification_jobs",
                column: "next_retry_at");

            migrationBuilder.CreateIndex(
                name: "ix_notification_jobs_status",
                table: "notification_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_oauth_accounts_provider_uid",
                table: "oauth_accounts",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oauth_accounts_user_id",
                table: "oauth_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_aggregate",
                table: "outbox_events",
                columns: new[] { "aggregate_id", "aggregate_name" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_created_at",
                table: "outbox_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_status",
                table: "outbox_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_passengers_booking_id",
                table: "passengers",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_events_payment_id",
                table: "payment_events",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_booking_id",
                table: "payments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payments_transaction_ref",
                table: "payments",
                column: "transaction_ref");

            migrationBuilder.CreateIndex(
                name: "ix_price_override_logs_admin_user_id",
                table: "price_override_logs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_override_logs_fare_price_id",
                table: "price_override_logs",
                column: "flight_fare_price_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_rules_route_active_priority",
                table: "price_rules",
                columns: new[] { "route_id", "is_active", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_status",
                table: "refunds",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_routes_destination_airport_id",
                table: "routes",
                column: "destination_airport_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_is_active",
                table: "routes",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_routes_origin_destination",
                table: "routes",
                columns: new[] { "origin_airport_id", "destination_airport_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_booking_item_id",
                table: "tickets",
                column: "booking_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_status",
                table: "tickets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_ticket_number",
                table: "tickets",
                column: "ticket_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_role",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_deleted",
                table: "users",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                table: "users",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_created_at",
                table: "wallet_ledger",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_payment_id",
                table: "wallet_ledger",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_user_id",
                table: "wallet_ledger",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "flight_inventories");

            migrationBuilder.DropTable(
                name: "idempotency_keys");

            migrationBuilder.DropTable(
                name: "notification_jobs");

            migrationBuilder.DropTable(
                name: "oauth_accounts");

            migrationBuilder.DropTable(
                name: "outbox_events");

            migrationBuilder.DropTable(
                name: "payment_events");

            migrationBuilder.DropTable(
                name: "price_override_logs");

            migrationBuilder.DropTable(
                name: "price_rules");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "wallet_ledger");

            migrationBuilder.DropTable(
                name: "flight_fare_prices");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "booking_items");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "fare_classes");

            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "passengers");

            migrationBuilder.DropTable(
                name: "aircrafts");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "airports");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
