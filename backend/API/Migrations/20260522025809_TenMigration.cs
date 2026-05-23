using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class TenMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketUpgradeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    BookingPassengerId = table.Column<int>(type: "integer", nullable: false),
                    FlightId = table.Column<int>(type: "integer", nullable: false),
                    FromSeatClassId = table.Column<int>(type: "integer", nullable: false),
                    ToSeatClassId = table.Column<int>(type: "integer", nullable: false),
                    FromInventoryId = table.Column<int>(type: "integer", nullable: false),
                    ToInventoryId = table.Column<int>(type: "integer", nullable: false),
                    PriceDifference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketUpgradeRequests", x => x.Id);
                    table.CheckConstraint("CK_TicketUpgradeRequest_PriceDifference_Positive", "\"PriceDifference\" > 0");
                    table.CheckConstraint("CK_TicketUpgradeRequest_Status_Valid", "\"Status\" IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_TicketUpgradeRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketUpgradeRequests_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketUpgradeRequests_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpgradeRequests_BookingId",
                table: "TicketUpgradeRequests",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpgradeRequests_PaymentId",
                table: "TicketUpgradeRequests",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpgradeRequests_Status_ExpiresAt",
                table: "TicketUpgradeRequests",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpgradeRequests_TicketId_Status",
                table: "TicketUpgradeRequests",
                columns: new[] { "TicketId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketUpgradeRequests");
        }
    }
}
