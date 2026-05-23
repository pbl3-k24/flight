using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class AddPartialTicketCancellation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "RefundRequests" ADD COLUMN IF NOT EXISTS "SourceAmountSnapshot" numeric(10,2) NULL;
                ALTER TABLE "RefundRequests" ADD COLUMN IF NOT EXISTS "TicketId" integer NULL;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Booking_Status_Valid') THEN
                        ALTER TABLE "Bookings" DROP CONSTRAINT "CK_Booking_Status_Valid";
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Booking_Status_Valid') THEN
                        ALTER TABLE "Bookings" ADD CONSTRAINT "CK_Booking_Status_Valid" CHECK ("Status" IN (0, 1, 2, 3, 4, 5));
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Ticket_Status_Valid') THEN
                        ALTER TABLE "Tickets" ADD CONSTRAINT "CK_Ticket_Status_Valid" CHECK ("Status" IN (0, 1, 2, 3, 4, 5));
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefundRequests_TicketId"
                ON "RefundRequests" ("TicketId")
                WHERE "TicketId" IS NOT NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_RefundRequests_Tickets_TicketId') THEN
                        ALTER TABLE "RefundRequests"
                        ADD CONSTRAINT "FK_RefundRequests_Tickets_TicketId"
                        FOREIGN KEY ("TicketId") REFERENCES "Tickets"("Id") ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Tickets_TicketId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_TicketId",
                table: "RefundRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ticket_Status_Valid",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_Status_Valid",
                table: "Bookings",
                sql: "\"Status\" IN (0, 1, 2, 3, 4)");

            migrationBuilder.DropColumn(
                name: "SourceAmountSnapshot",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "RefundRequests");
        }
    }
}
