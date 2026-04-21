using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraintsToAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_SeatClass_ChangeFee_NonNegative",
                table: "SeatClasses",
                sql: "\"ChangeFee\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SeatClass_RefundPercent_Valid",
                table: "SeatClasses",
                sql: "\"RefundPercent\" >= 0 AND \"RefundPercent\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Route_DifferentAirports",
                table: "Routes",
                sql: "\"DepartureAirportId\" != \"ArrivalAirportId\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Route_DistanceKm_Positive",
                table: "Routes",
                sql: "\"DistanceKm\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Route_EstimatedDurationMinutes_Positive",
                table: "Routes",
                sql: "\"EstimatedDurationMinutes\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefundRequest_RefundAmount_Positive",
                table: "RefundRequests",
                sql: "\"RefundAmount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefundPolicy_HoursBeforeDeparture_Positive",
                table: "RefundPolicies",
                sql: "\"HoursBeforeDeparture\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefundPolicy_PenaltyFee_NonNegative",
                table: "RefundPolicies",
                sql: "\"PenaltyFee\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefundPolicy_RefundPercent_Valid",
                table: "RefundPolicies",
                sql: "\"RefundPercent\" >= 0 AND \"RefundPercent\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Promotion_DiscountValue_Positive",
                table: "Promotions",
                sql: "\"DiscountValue\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Promotion_UsedCount_NonNegative",
                table: "Promotions",
                sql: "\"UsedCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Amount_Positive",
                table: "Payments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_AvailableSeats_Valid",
                table: "FlightSeatInventories",
                sql: "\"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"TotalSeats\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_BasePrice_Positive",
                table: "FlightSeatInventories",
                sql: "\"BasePrice\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_CurrentPrice_Positive",
                table: "FlightSeatInventories",
                sql: "\"CurrentPrice\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_HeldSeats_NonNegative",
                table: "FlightSeatInventories",
                sql: "\"HeldSeats\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_Seats_Total",
                table: "FlightSeatInventories",
                sql: "\"AvailableSeats\" + \"HeldSeats\" + \"SoldSeats\" <= \"TotalSeats\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_SoldSeats_NonNegative",
                table: "FlightSeatInventories",
                sql: "\"SoldSeats\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlightSeatInventory_TotalSeats_Positive",
                table: "FlightSeatInventories",
                sql: "\"TotalSeats\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_DiscountAmount_NonNegative",
                table: "Bookings",
                sql: "\"DiscountAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_FinalAmount_LessThanTotal",
                table: "Bookings",
                sql: "\"FinalAmount\" <= \"TotalAmount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_FinalAmount_Positive",
                table: "Bookings",
                sql: "\"FinalAmount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_TotalAmount_Positive",
                table: "Bookings",
                sql: "\"TotalAmount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AircraftSeatTemplate_DefaultBasePrice_Positive",
                table: "AircraftSeatTemplates",
                sql: "\"DefaultBasePrice\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AircraftSeatTemplate_DefaultSeatCount_Positive",
                table: "AircraftSeatTemplates",
                sql: "\"DefaultSeatCount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Aircraft_TotalSeats_Positive",
                table: "Aircraft",
                sql: "\"TotalSeats\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SeatClass_ChangeFee_NonNegative",
                table: "SeatClasses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SeatClass_RefundPercent_Valid",
                table: "SeatClasses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Route_DifferentAirports",
                table: "Routes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Route_DistanceKm_Positive",
                table: "Routes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Route_EstimatedDurationMinutes_Positive",
                table: "Routes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefundRequest_RefundAmount_Positive",
                table: "RefundRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefundPolicy_HoursBeforeDeparture_Positive",
                table: "RefundPolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefundPolicy_PenaltyFee_NonNegative",
                table: "RefundPolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefundPolicy_RefundPercent_Valid",
                table: "RefundPolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Promotion_DiscountValue_Positive",
                table: "Promotions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Promotion_UsedCount_NonNegative",
                table: "Promotions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_AvailableSeats_Valid",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_BasePrice_Positive",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_CurrentPrice_Positive",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_HeldSeats_NonNegative",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_Seats_Total",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_SoldSeats_NonNegative",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlightSeatInventory_TotalSeats_Positive",
                table: "FlightSeatInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_DiscountAmount_NonNegative",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_FinalAmount_LessThanTotal",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_FinalAmount_Positive",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_TotalAmount_Positive",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AircraftSeatTemplate_DefaultBasePrice_Positive",
                table: "AircraftSeatTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AircraftSeatTemplate_DefaultSeatCount_Positive",
                table: "AircraftSeatTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Aircraft_TotalSeats_Positive",
                table: "Aircraft");
        }
    }
}
