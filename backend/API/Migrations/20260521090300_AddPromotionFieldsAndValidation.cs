using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionFieldsAndValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Promotions"
                    ADD COLUMN IF NOT EXISTS "Description" character varying(1000) NULL,
                    ADD COLUMN IF NOT EXISTS "MinimumAmount" numeric(10,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NULL;

                ALTER TABLE "Promotions"
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_DiscountType_Valid",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_Percentage_Max100",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_MinimumAmount_NonNegative",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_ValidDateRange";

                ALTER TABLE "Promotions"
                    ADD CONSTRAINT "CK_Promotion_DiscountType_Valid"
                        CHECK ("DiscountType" IN (0, 1)),
                    ADD CONSTRAINT "CK_Promotion_Percentage_Max100"
                        CHECK ("DiscountType" <> 0 OR "DiscountValue" <= 100),
                    ADD CONSTRAINT "CK_Promotion_MinimumAmount_NonNegative"
                        CHECK ("MinimumAmount" >= 0),
                    ADD CONSTRAINT "CK_Promotion_ValidDateRange"
                        CHECK ("ValidFrom" < "ValidTo");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Promotions"
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_DiscountType_Valid",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_Percentage_Max100",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_MinimumAmount_NonNegative",
                    DROP CONSTRAINT IF EXISTS "CK_Promotion_ValidDateRange";

                ALTER TABLE "Promotions"
                    DROP COLUMN IF EXISTS "UpdatedAt",
                    DROP COLUMN IF EXISTS "MinimumAmount",
                    DROP COLUMN IF EXISTS "Description";
                """);
        }
    }
}
