using API.Application.Services;
using API.Domain.Entities;
using API.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests;

public class BookingAndPaymentRegressionTests
{
    [Fact]
    public void CancelBooking_WhenValid_UpdatesStatusAndCancelledAt()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Confirmed,
            TotalPrice = 100m,
            Flight = new Flight
            {
                DepartureTime = DateTime.UtcNow.AddHours(48)
            }
        };

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.CancelledAt);
    }

    [Fact]
    public void CancelBooking_WhenPending_Throws()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Pending,
            Flight = new Flight
            {
                DepartureTime = DateTime.UtcNow.AddHours(48)
            }
        };

        Assert.Throws<InvalidOperationException>(() => booking.Cancel());
    }

    [Theory]
    [InlineData(1000, 80, 0.20, 1000)] // full refund
    [InlineData(1000, 48, 0.20, 900)]  // 10% fee
    [InlineData(1000, 12, 0.20, 800)]  // 20% fee
    public void CalculateRefundAmount_UsesExpectedPolicy(
        decimal amount,
        int hoursUntilDeparture,
        decimal cancellationFee,
        decimal expected)
    {
        var service = new PaymentService(NullLogger<PaymentService>.Instance);

        var refund = service.CalculateRefundAmount(amount, hoursUntilDeparture, cancellationFee);

        Assert.Equal(expected, refund);
    }
}
