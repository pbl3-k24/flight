using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class UpdatePaymentStatusConstraintForPendingRefund : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Payment_Status_Valid') THEN
                        ALTER TABLE "Payments" DROP CONSTRAINT "CK_Payment_Status_Valid";
                    END IF;
                    ALTER TABLE "Payments"
                    ADD CONSTRAINT "CK_Payment_Status_Valid" CHECK ("Status" IN (0, 1, 2, 3, 4, 5));
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Payment_Status_Valid') THEN
                        ALTER TABLE "Payments" DROP CONSTRAINT "CK_Payment_Status_Valid";
                    END IF;
                    ALTER TABLE "Payments"
                    ADD CONSTRAINT "CK_Payment_Status_Valid" CHECK ("Status" IN (0, 1, 2, 3, 4));
                END $$;
                """);
        }
    }
}
