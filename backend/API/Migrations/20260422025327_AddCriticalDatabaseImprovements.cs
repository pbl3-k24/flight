using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalDatabaseImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add concurrency tokens (Version) to entities that need it
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Flights",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RefundRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Promotions",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("ConcurrencyToken", true);

            // Add soft delete support
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Flights",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Flights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RefundRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RefundRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Promotions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Promotions",
                type: "timestamp with time zone",
                nullable: true);

            // Add audit properties (CreatedBy, UpdatedBy)
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Flights",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Flights",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "RefundRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "RefundRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Promotions",
                type: "integer",
                nullable: true);

            // Add missing User properties
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DateOfBirth",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportExpiryDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportCountry",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCurrency",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingOptIn",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewsletterSubscription",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotificationPreferences",
                table: "Users",
                type: "jsonb",
                nullable: true);

            // Add missing AuditLog properties
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "AuditLogs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // Add Currency property to Payment and Booking
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Bookings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "VND");

            // Add missing indexes
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Status",
                table: "Flights",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_Status",
                table: "RefundRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Status",
                table: "NotificationLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive",
                table: "Promotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassengers_FlightSeatInventoryId",
                table: "BookingPassengers",
                column: "FlightSeatInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_BookingPassengerId",
                table: "BookingServices",
                column: "BookingPassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_BookingId",
                table: "PromotionUsages",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_SeatClassId",
                table: "RefundPolicies",
                column: "SeatClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ReplacedByTicketId",
                table: "Tickets",
                column: "ReplacedByTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            // Add CHECK constraints for status fields
            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" ADD CONSTRAINT ""CK_Booking_TripType_Valid""
                CHECK (""TripType"" IN (0, 1));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" ADD CONSTRAINT ""CK_Booking_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2, 3));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Flights"" ADD CONSTRAINT ""CK_Flight_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2, 3));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Payments"" ADD CONSTRAINT ""CK_Payment_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2, 3));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""RefundRequests"" ADD CONSTRAINT ""CK_RefundRequest_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2, 3));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""NotificationLogs"" ADD CONSTRAINT ""CK_NotificationLog_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" ADD CONSTRAINT ""CK_User_Status_Valid""
                CHECK (""Status"" IN (0, 1, 2));
            ");

            // Add CHECK constraint for ContactEmail default
            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" ADD CONSTRAINT ""CK_Booking_ContactEmail_Default""
                CHECK (""ContactEmail"" IS NOT NULL);
            ");

            // Add CHECK constraint for NotificationLog Email default
            migrationBuilder.Sql(@"
                ALTER TABLE ""NotificationLogs"" ADD CONSTRAINT ""CK_NotificationLog_Email_Default""
                CHECK (""Email"" IS NOT NULL OR ""UserId"" IS NOT NULL);
            ");

            // Add CHECK constraint for Token UsedAt validation
            migrationBuilder.Sql(@"
                ALTER TABLE ""EmailVerificationTokens"" ADD CONSTRAINT ""CK_EmailVerificationToken_UsedAt_Valid""
                CHECK (""UsedAt"" IS NULL OR ""UsedAt"" <= ""ExpiresAt"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""PasswordResetTokens"" ADD CONSTRAINT ""CK_PasswordResetToken_UsedAt_Valid""
                CHECK (""UsedAt"" IS NULL OR ""UsedAt"" <= ""ExpiresAt"");
            ");

            // Add CHECK constraint for RefundRequest ProcessedAt
            migrationBuilder.Sql(@"
                ALTER TABLE ""RefundRequests"" ADD CONSTRAINT ""CK_RefundRequest_ProcessedAt_Valid""
                CHECK (
                    (""Status"" <> 2 AND ""ProcessedAt"" IS NULL) OR
                    (""Status"" = 2 AND ""ProcessedAt"" IS NOT NULL)
                );
            ");

            // Add CHECK constraint for Booking ContactPhone default
            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" ADD CONSTRAINT ""CK_Booking_ContactPhone_Default""
                CHECK (""ContactPhone"" IS NULL OR ""ContactPhone"" <> '');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop CHECK constraints
            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS ""CK_Booking_ContactPhone_Default"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""RefundRequests"" DROP CONSTRAINT IF EXISTS ""CK_RefundRequest_ProcessedAt_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""PasswordResetTokens"" DROP CONSTRAINT IF EXISTS ""CK_PasswordResetToken_UsedAt_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""EmailVerificationTokens"" DROP CONSTRAINT IF EXISTS ""CK_EmailVerificationToken_UsedAt_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""NotificationLogs"" DROP CONSTRAINT IF EXISTS ""CK_NotificationLog_Email_Default"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS ""CK_Booking_ContactEmail_Default"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS ""CK_Booking_Status_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS ""CK_Booking_TripType_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Flights"" DROP CONSTRAINT IF EXISTS ""CK_Flight_Status_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Payments"" DROP CONSTRAINT IF EXISTS ""CK_Payment_Status_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""RefundRequests"" DROP CONSTRAINT IF EXISTS ""CK_RefundRequest_Status_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""NotificationLogs"" DROP CONSTRAINT IF EXISTS ""CK_NotificationLog_Status_Valid"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_User_Status_Valid"";
            ");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ReplacedByTicketId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_RefundPolicies_SeatClassId",
                table: "RefundPolicies");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_BookingId",
                table: "PromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_BookingServices_BookingPassengerId",
                table: "BookingServices");

            migrationBuilder.DropIndex(
                name: "IX_BookingPassengers_FlightSeatInventoryId",
                table: "BookingPassengers");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_IsActive",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Status",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_Status",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Flights_Status",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            // Drop Currency columns
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Payments");

            // Drop AuditLog extra properties
            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            // Drop User extra properties
            migrationBuilder.DropColumn(
                name: "NotificationPreferences",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NewsletterSubscription",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarketingOptIn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredCurrency",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PassportCountry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PassportExpiryDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            // Drop audit properties
            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            // Drop soft delete columns
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            // Drop Version columns
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Users");
        }
    }
}
