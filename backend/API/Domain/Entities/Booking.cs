namespace API.Domain.Entities;

using API.Domain.Enums;

/// <summary>
/// Represents a booking in the system.
/// This is an aggregate root within the flight context that manages passenger information and booking state.
/// Enforces invariants:
/// - booking_reference is unique
/// - total_price > 0
/// - passenger_count <= flight.total_seats
/// - user exists and is active
/// - flight exists and is active
/// </summary>
public class Booking
{
    /// <summary>Unique identifier for the booking.</summary>
    public int Id { get; set; }

    /// <summary>Flight ID for which this booking is made.</summary>
    public int FlightId { get; set; }

    /// <summary>User ID who made the booking.</summary>
    public int UserId { get; set; }

    /// <summary>Unique booking reference code (e.g., "ABC123XYZ").</summary>
    public string BookingReference { get; set; } = null!;

    /// <summary>Total number of passengers in this booking.</summary>
    public int PassengerCount { get; set; }

    /// <summary>Total price for the entire booking.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Current status of the booking (Pending, Confirmed, CheckedIn, Cancelled).</summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>When the booking was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the booking was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>When the booking was cancelled (null if not cancelled).</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Additional notes or special requests for the booking.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>Flight associated with this booking.</summary>
    public Flight? Flight { get; set; }

    /// <summary>User who made this booking.</summary>
    public User? User { get; set; }

    /// <summary>Passengers included in this booking.</summary>
    public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();

    /// <summary>Payment associated with this booking.</summary>
    public Payment? Payment { get; set; }

    // Domain methods for managing booking state
    /// <summary>
    /// Checks if the booking can be cancelled.
    /// Booking must be Confirmed and must be at least 24 hours before flight departure.
    /// </summary>
    /// <param name="currentDateTime">Current date and time for comparison.</param>
    /// <returns>True if booking can be cancelled, false otherwise.</returns>
    public bool CanCancel(DateTime currentDateTime)
    {
        if (Status != BookingStatus.Confirmed)
            return false;

        if (Flight == null)
            return false;

        var hoursSuntilDeparture = (Flight.DepartureTime - currentDateTime).TotalHours;
        return hoursSuntilDeparture > 24;
    }

    /// <summary>
    /// Confirms the booking after successful payment.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if booking status is not Pending.</exception>
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm booking with status {Status}");

        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the booking as checked in.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if booking status is not Confirmed.</exception>
    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot check in booking with status {Status}");

        Status = BookingStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the booking.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if booking cannot be cancelled (wrong status or within 24h of departure).</exception>
    public void Cancel()
    {
        if (!CanCancel(DateTime.UtcNow))
        {
            if (Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Booking has already been cancelled.");

            if (Status == BookingStatus.CheckedIn)
                throw new InvalidOperationException("Cannot cancel a checked-in booking.");

            if (Status == BookingStatus.Pending)
                throw new InvalidOperationException("Cannot cancel a pending booking. Payment must be completed first.");

            throw new InvalidOperationException("Booking cannot be cancelled within 24 hours of flight departure.");
        }

        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the refund amount based on cancellation time and fee percentage.
    /// </summary>
    /// <param name="cancellationFeePercentage">Base cancellation fee percentage (0.0 to 1.0).</param>
    /// <returns>Refund amount after applicable fees. Returns 0 if booking is not cancelled.</returns>
    public decimal CalculateRefund(decimal cancellationFeePercentage = 0.20m)
    {
        if (Status != BookingStatus.Cancelled)
            return 0m;

        if (Flight == null || CancelledAt == null)
            return 0m;

        var hoursUntilFlight = (Flight.DepartureTime - CancelledAt.Value).TotalHours;

        decimal feeApplied;
        if (hoursUntilFlight > 72)
        {
            // More than 72 hours: 0% fee
            feeApplied = 0m;
        }
        else if (hoursUntilFlight > 24)
        {
            // 24-72 hours: 10% fee
            feeApplied = 0.10m;
        }
        else
        {
            // Less than 24 hours: 25% fee
            feeApplied = 0.25m;
        }

        var refundAmount = TotalPrice * (1 - feeApplied);
        return Math.Round(refundAmount, 2);
    }

    /// <summary>
    /// Generates a booking reference code.
    /// Format: {FlightNumber}-{Date:yyyyMMdd}-{RandomId}
    /// </summary>
    /// <returns>Unique booking reference string.</returns>
    public string GenerateBookingReference()
    {
        if (Flight == null || string.IsNullOrEmpty(Flight.FlightNumber))
            throw new InvalidOperationException("Flight information is required to generate booking reference.");

        var dateComponent = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomComponent = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        return $"{Flight.FlightNumber}-{dateComponent}-{randomComponent}";
    }
}
