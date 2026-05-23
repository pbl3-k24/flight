using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class AddNotificationInboxFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "NotificationLogs" ADD COLUMN IF NOT EXISTS "IsRead" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "NotificationLogs" ADD COLUMN IF NOT EXISTS "ReadAt" timestamp with time zone NULL;
                ALTER TABLE "NotificationLogs" ADD COLUMN IF NOT EXISTS "Category" character varying(50) NOT NULL DEFAULT 'GENERAL';
                ALTER TABLE "NotificationLogs" ADD COLUMN IF NOT EXISTS "RelatedEntityType" character varying(100) NULL;
                ALTER TABLE "NotificationLogs" ADD COLUMN IF NOT EXISTS "RelatedEntityId" integer NULL;

                CREATE INDEX IF NOT EXISTS "IX_NotificationLogs_UserId_IsRead"
                ON "NotificationLogs" ("UserId", "IsRead");

                CREATE INDEX IF NOT EXISTS "IX_NotificationLogs_Category"
                ON "NotificationLogs" ("Category");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_NotificationLogs_UserId_IsRead";
                DROP INDEX IF EXISTS "IX_NotificationLogs_Category";

                ALTER TABLE "NotificationLogs" DROP COLUMN IF EXISTS "RelatedEntityId";
                ALTER TABLE "NotificationLogs" DROP COLUMN IF EXISTS "RelatedEntityType";
                ALTER TABLE "NotificationLogs" DROP COLUMN IF EXISTS "Category";
                ALTER TABLE "NotificationLogs" DROP COLUMN IF EXISTS "ReadAt";
                ALTER TABLE "NotificationLogs" DROP COLUMN IF EXISTS "IsRead";
                """);
        }
    }
}
