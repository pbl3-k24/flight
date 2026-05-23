using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_BookingPassengerId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_PaymentId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_UserId",
                table: "NotificationLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "FlightDisruptionDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AffectedFlightId = table.Column<int>(type: "integer", nullable: false),
                    LegType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DecisionType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DecisionDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewFlightId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightDisruptionDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightDisruptionDecisions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlightDisruptionDecisions_Flights_AffectedFlightId",
                        column: x => x.AffectedFlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightDisruptionDecisions_Flights_NewFlightId",
                        column: x => x.NewFlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightDisruptionDecisions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_BookingPassengerId",
                table: "Tickets",
                column: "BookingPassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_PaymentId",
                table: "RefundRequests",
                column: "PaymentId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings",
                sql: "\"Status\" IN (0, 1, 2, 3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_FlightDisruptionDecisions_AffectedFlightId",
                table: "FlightDisruptionDecisions",
                column: "AffectedFlightId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightDisruptionDecisions_BookingId_AffectedFlightId_LegType",
                table: "FlightDisruptionDecisions",
                columns: new[] { "BookingId", "AffectedFlightId", "LegType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightDisruptionDecisions_DecisionDeadline",
                table: "FlightDisruptionDecisions",
                column: "DecisionDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_FlightDisruptionDecisions_NewFlightId",
                table: "FlightDisruptionDecisions",
                column: "NewFlightId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightDisruptionDecisions_UserId_Status",
                table: "FlightDisruptionDecisions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightDisruptionDecisions");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_BookingPassengerId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_PaymentId",
                table: "RefundRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_BookingPassengerId",
                table: "Tickets",
                column: "BookingPassengerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_PaymentId",
                table: "RefundRequests",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UserId",
                table: "NotificationLogs",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings",
                sql: "\"Status\" IN (0, 1, 2, 3, 4, 5)");
        }
    }
}
