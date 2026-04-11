namespace FlightBooking.Application.Services.Interfaces;

public interface IInventoryReservationService
{
    /// <summary>Hold seats for a booking (with timeout). Returns false if not enough seats.</summary>
    Task<bool> HoldSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId, TimeSpan holdDuration);

    /// <summary>Confirm seat hold after successful payment (deduct from available).</summary>
    Task ConfirmSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId);

    /// <summary>Release held seats (on payment failure or booking expiry).</summary>
    Task ReleaseSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId);

    /// <summary>Release all expired booking holds (called by scheduler).</summary>
    Task ReleaseExpiredHoldsAsync();
}
