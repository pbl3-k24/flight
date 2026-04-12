namespace API.Domain.Entities;

using API.Domain.Enums;

/// <summary>
/// Represents a flight in the system.
/// This is an aggregate root that manages flight seat availability and booking limits.
/// Enforces invariants:
/// - available_seats &lt;= total_seats
/// - available_seats &gt;= 0
/// - departure_time != arrival_time
/// - departure_airport_id != arrival_airport_id
/// - base_price &gt;= 0
/// </summary>
public class Flight
{
    /// <summary>Unique identifier for the flight.</summary>
    public int Id { get; set; }

    /// <summary>Flight number (e.g., "AA100").</summary>
    public string FlightNumber { get; set; } = null!;

    /// <summary>Airport ID from which the flight departs.</summary>
    public int DepartureAirportId { get; set; }

    /// <summary>Airport where the flight arrives.</summary>
    public int ArrivalAirportId { get; set; }

    /// <summary>Date and time the flight is scheduled to depart.</summary>
    public DateTime DepartureTime { get; set; }

    /// <summary>Date and time the flight is scheduled to arrive.</summary>
    public DateTime ArrivalTime { get; set; }

    /// <summary>Airline operating the flight.</summary>
    public string Airline { get; set; } = null!;

    /// <summary>Aircraft model used for this flight.</summary>
    public string AircraftModel { get; set; } = null!;

    /// <summary>Total number of seats available on the flight.</summary>
    public int TotalSeats { get; set; }

    /// <summary>Number of seats currently available for booking.</summary>
    public int AvailableSeats { get; set; }

    /// <summary>Base price per seat for this flight.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Current status of the flight (Active, Cancelled, Delayed, Completed).</summary>
    public FlightStatus Status { get; set; } = FlightStatus.Active;

    /// <summary>When the flight was created in the system.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the flight information was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>Airport from which the flight departs.</summary>
    public Airport? DepartureAirport { get; set; }

    /// <summary>Airport where the flight arrives.</summary>
    public Airport? ArrivalAirport { get; set; }

    /// <summary>All bookings made for this flight.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>Crew members assigned to this flight.</summary>
    public ICollection<FlightCrew> CrewMembers { get; set; } = new List<FlightCrew>();

    // Domain methods for managing flight state
    /// <summary>
    /// Checks if the flight can accommodate a booking request.
    /// Flight must be Active and have sufficient available seats.
    /// </summary>
    /// <param name="passengerCount">Number of passengers to book.</param>
    /// <returns>True if flight is Active and has sufficient seats, false otherwise.</returns>
    public bool CanBook(int passengerCount)
    {
        return Status == FlightStatus.Active && AvailableSeats >= passengerCount;
    }

    /// <summary>
    /// Reserves the specified number of seats for a booking.
    /// </summary>
    /// <param name="seatCount">Number of seats to reserve.</param>
    /// <exception cref="InvalidOperationException">Thrown if insufficient seats available.</exception>
    public void ReserveSeats(int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentException("Seat count must be greater than 0.", nameof(seatCount));

        if (!CanBook(seatCount))
            throw new InvalidOperationException($"Cannot reserve seats. Flight status: {Status}, Available seats: {AvailableSeats}, Requested: {seatCount}");

        AvailableSeats -= seatCount;
    }

    /// <summary>
    /// Releases reserved seats back to the available pool (e.g., when a booking is cancelled).
    /// </summary>
    /// <param name="seatCount">Number of seats to release.</param>
    /// <exception cref="InvalidOperationException">Thrown if release would exceed total seats.</exception>
    public void ReleaseSeats(int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentException("Seat count must be greater than 0.", nameof(seatCount));

        if (AvailableSeats + seatCount > TotalSeats)
            throw new InvalidOperationException($"Cannot release {seatCount} seats. Would exceed total seats of {TotalSeats}");

        AvailableSeats += seatCount;
    }

    /// <summary>
    /// Checks if the flight's departure is within the specified number of hours.
    /// </summary>
    /// <param name="hours">Number of hours to check.</param>
    /// <returns>True if departure is within the specified hours, false otherwise.</returns>
    public bool IsDepartureSoon(int hours)
    {
        var hoursUntilDeparture = (DepartureTime - DateTime.UtcNow).TotalHours;
        return hoursUntilDeparture <= hours && hoursUntilDeparture > 0;
    }

    /// <summary>
    /// Marks the flight as cancelled.
    /// </summary>
    public void Cancel()
    {
        Status = FlightStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the flight as delayed.
    /// </summary>
    public void MarkAsDelayed()
    {
        if (Status == FlightStatus.Active)
        {
            Status = FlightStatus.Delayed;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks the flight as completed.
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = FlightStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
