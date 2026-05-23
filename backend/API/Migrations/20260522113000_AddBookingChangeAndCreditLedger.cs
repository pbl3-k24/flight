using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    public partial class AddBookingChangeAndCreditLedger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCreditLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCreditLedgers", x => x.Id);
                    table.CheckConstraint("CK_UserCreditLedger_Amount_NonNegative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_UserCreditLedgers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    LegType = table.Column<int>(type: "integer", nullable: false),
                    OldFlightId = table.Column<int>(type: "integer", nullable: false),
                    NewFlightId = table.Column<int>(type: "integer", nullable: false),
                    OldAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NewAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ChangeFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingChangeRequests", x => x.Id);
                    table.CheckConstraint("CK_BookingChangeRequest_Status_Valid", "\"Status\" IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_BookingChangeRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingChangeRequests_Flights_NewFlightId",
                        column: x => x.NewFlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingChangeRequests_Flights_OldFlightId",
                        column: x => x.OldFlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingChangeRequests_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_BookingId",
                table: "BookingChangeRequests",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_BookingId_LegType_Status",
                table: "BookingChangeRequests",
                columns: new[] { "BookingId", "LegType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_NewFlightId",
                table: "BookingChangeRequests",
                column: "NewFlightId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_OldFlightId",
                table: "BookingChangeRequests",
                column: "OldFlightId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_PaymentId",
                table: "BookingChangeRequests",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditLedgers_CreatedAt",
                table: "UserCreditLedgers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditLedgers_UserId",
                table: "UserCreditLedgers",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingChangeRequests");

            migrationBuilder.DropTable(
                name: "UserCreditLedgers");
        }
    }
}

