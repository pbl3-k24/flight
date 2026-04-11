using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class RefundPolicyService(IBookingRepository bookingRepository, IFlightRepository flightRepository) : IRefundPolicyService
{
    public async Task<RefundEligibilityDto> EvaluateEligibilityAsync(Guid bookingItemId)
    {
        var item = await bookingRepository.GetItemByIdAsync(bookingItemId)
            ?? throw new KeyNotFoundException($"Booking item {bookingItemId} not found.");

        if (item.Status == "cancelled")
            return new RefundEligibilityDto(false, 0, 0, "Booking item is already cancelled.");

        if (item.Ticket?.Status == "used")
            return new RefundEligibilityDto(false, 0, 0, "Ticket has already been used.");

        var flight = await flightRepository.GetByIdAsync(item.FlightId)
            ?? throw new KeyNotFoundException($"Flight {item.FlightId} not found.");

        var hoursUntilDeparture = (flight.DepartureTime - DateTime.UtcNow).TotalHours;

        if (hoursUntilDeparture < 0)
            return new RefundEligibilityDto(false, 0, 0, "Flight has already departed.");

        // Policy based on fare class refundability
        if (!item.FareClass.IsRefundable)
            return new RefundEligibilityDto(false, 0, item.Price + item.TaxAndFee, "Fare class is non-refundable.");

        var cancellationFee = await CalculateCancellationFeeAsync(bookingItemId);
        var refundableAmount = (item.Price + item.TaxAndFee) - cancellationFee;

        if (refundableAmount <= 0)
            return new RefundEligibilityDto(false, 0, cancellationFee, "Cancellation fee exceeds ticket value.");

        return new RefundEligibilityDto(true, refundableAmount, cancellationFee, null);
    }

    public async Task<decimal> CalculateCancellationFeeAsync(Guid bookingItemId)
    {
        var item = await bookingRepository.GetItemByIdAsync(bookingItemId)
            ?? throw new KeyNotFoundException($"Booking item {bookingItemId} not found.");

        var flight = await flightRepository.GetByIdAsync(item.FlightId)
            ?? throw new KeyNotFoundException($"Flight {item.FlightId} not found.");

        var hoursUntilDeparture = (flight.DepartureTime - DateTime.UtcNow).TotalHours;
        var totalPrice = item.Price + item.TaxAndFee;

        // Fee schedule based on fareClass.RefundFeePercent and time-to-departure
        var feePercent = hoursUntilDeparture switch
        {
            >= 72 => item.FareClass.RefundFeePercent,
            >= 24 => item.FareClass.RefundFeePercent + 0.10m,
            >= 4 => item.FareClass.RefundFeePercent + 0.20m,
            _ => 1.00m // 100% fee - non-refundable within 4 hours
        };

        return Math.Round(totalPrice * feePercent, 0);
    }
}
