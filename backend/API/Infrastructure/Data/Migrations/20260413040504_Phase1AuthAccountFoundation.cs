using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1AuthAccountFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "IATA airport code (e.g., LAX, JFK)"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Full airport name"),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "City location"),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Country location"),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Airport timezone (e.g., America/Los_Angeles)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrewMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Crew member first name"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Crew member last name"),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Job role (Pilot, Co-Pilot, Flight Attendant, etc.)"),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "License/certification number"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Unique email address"),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Full display name"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "First name"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Last name"),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Date of birth"),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Optional phone number"),
                    GoogleId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "External Google identity ID"),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Hashed password"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Account status (Active, Deactivated, Suspended)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Email_NotEmpty", "length(trim(\"Email\")) > 0");
                    table.CheckConstraint("CK_Users_FullName_NotEmpty", "length(trim(\"FullName\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Flight designation (e.g., AA100)"),
                    DepartureAirportId = table.Column<int>(type: "integer", nullable: false),
                    ArrivalAirportId = table.Column<int>(type: "integer", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Scheduled departure time"),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Scheduled arrival time"),
                    Airline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Airline name"),
                    AircraftModel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Aircraft type"),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false, comment: "Total seat capacity"),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false, comment: "Currently available seats"),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Base price per seat"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Flight status (Active, Cancelled, Delayed, Completed)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.CheckConstraint("CK_Flights_AvailableSeats_Range", "\"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"TotalSeats\"");
                    table.CheckConstraint("CK_Flights_BasePrice_NonNegative", "\"BasePrice\" >= 0");
                    table.CheckConstraint("CK_Flights_Route_DifferentAirports", "\"DepartureAirportId\" <> \"ArrivalAirportId\"");
                    table.CheckConstraint("CK_Flights_Time_Order", "\"DepartureTime\" < \"ArrivalTime\"");
                    table.CheckConstraint("CK_Flights_TotalSeats_Positive", "\"TotalSeats\" > 0");
                    table.ForeignKey(
                        name: "FK_Flights_ArrivalAirportId",
                        column: x => x.ArrivalAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_DepartureAirportId",
                        column: x => x.DepartureAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                    table.CheckConstraint("CK_EmailVerificationTokens_UsedAfterIssue", "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");
                    table.ForeignKey(
                        name: "FK_EmailVerificationTokens_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.CheckConstraint("CK_PasswordResetTokens_UsedAfterIssue", "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BookingReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Unique booking reference code (e.g., AA100-20260415-ABC123)"),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false, comment: "Number of passengers"),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total booking price"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Booking status (Pending, Confirmed, CheckedIn, Cancelled)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp"),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Cancellation timestamp (null if not cancelled)"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Optional notes or special requests")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_PassengerCount_Positive", "\"PassengerCount\" > 0");
                    table.CheckConstraint("CK_Bookings_TotalPrice_NonNegative", "\"TotalPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_Bookings_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlightCrews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightId = table.Column<int>(type: "integer", nullable: false),
                    CrewMemberId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Assignment timestamp"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightCrews", x => x.Id);
                    table.UniqueConstraint("AK_FlightCrews_FlightId_CrewMemberId", x => new { x.FlightId, x.CrewMemberId });
                    table.ForeignKey(
                        name: "FK_FlightCrews_CrewMemberId",
                        column: x => x.CrewMemberId,
                        principalTable: "CrewMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlightCrews_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Passengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Passenger first name"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Passenger last name"),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Passenger date of birth"),
                    PassportNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Optional passport number"),
                    Nationality = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, comment: "Optional nationality code (ISO 3166-1)"),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Passenger email address"),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Optional phone number"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passengers_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Payment amount"),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD", comment: "Currency code (e.g., USD, EUR)"),
                    Method = table.Column<int>(type: "integer", nullable: false, comment: "Payment method (CreditCard, DebitCard, DigitalWallet, BankTransfer)"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Payment status (Pending, Processing, Completed, Failed, Refunded)"),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "External transaction ID from payment gateway"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())", comment: "Creation timestamp"),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Processing/completion timestamp"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Additional notes or error messages"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_Payments_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "System administrator", "Admin" },
                    { 2, "Operational staff", "Staff" },
                    { 3, "Default customer role", "Customer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_City",
                table: "Airports",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_Code",
                table: "Airports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_Country",
                table: "Airports",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingReference",
                table: "Bookings",
                column: "BookingReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedAt",
                table: "Bookings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId",
                table: "Bookings",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId_Status",
                table: "Bookings",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrewMembers_LicenseNumber",
                table: "CrewMembers",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrewMembers_Name",
                table: "CrewMembers",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_CrewMembers_Role",
                table: "CrewMembers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_Code",
                table: "EmailVerificationTokens",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserId",
                table: "EmailVerificationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightCrews_CrewMemberId",
                table: "FlightCrews",
                column: "CrewMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightCrews_FlightId",
                table: "FlightCrews",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightCrews_FlightId_CrewMemberId",
                table: "FlightCrews",
                columns: new[] { "FlightId", "CrewMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_ArrivalAirportId",
                table: "Flights",
                column: "ArrivalAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DepartureAirportId",
                table: "Flights",
                column: "DepartureAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DepartureTime",
                table: "Flights",
                column: "DepartureTime");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_FlightNumber",
                table: "Flights",
                column: "FlightNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Route_DepartureTime",
                table: "Flights",
                columns: new[] { "DepartureAirportId", "ArrivalAirportId", "DepartureTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Status",
                table: "Flights",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_BookingId",
                table: "Passengers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_Email",
                table: "Passengers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_Name",
                table: "Passengers",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Code",
                table: "PasswordResetTokens",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.DropTable(
                name: "FlightCrews");

            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "CrewMembers");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Airports");
        }
    }
}
