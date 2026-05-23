using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class AddPromotionMaxDiscountAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Promotions"
                ADD COLUMN IF NOT EXISTS "MaxDiscountAmount" numeric(10,2) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Promotions"
                DROP COLUMN IF EXISTS "MaxDiscountAmount";
                """);
        }
    }
}
