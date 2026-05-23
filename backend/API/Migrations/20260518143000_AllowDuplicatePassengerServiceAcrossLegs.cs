using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class AllowDuplicatePassengerServiceAcrossLegs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_BookingServices_BookingPassengerId_AdditionalServiceId";
                CREATE INDEX IF NOT EXISTS "IX_BookingServices_BookingPassengerId_AdditionalServiceId"
                ON "BookingServices" ("BookingPassengerId", "AdditionalServiceId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_BookingServices_BookingPassengerId_AdditionalServiceId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_BookingServices_BookingPassengerId_AdditionalServiceId"
                ON "BookingServices" ("BookingPassengerId", "AdditionalServiceId")
                WHERE "IsDeleted" = false;
                """);
        }
    }
}
