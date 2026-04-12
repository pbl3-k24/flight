namespace API.Application.DTOs;

/// <summary>
/// DTO for the response when returning booking information.
/// Transfers data from the application layer to the API layer.
/// </summary>
public class BookingResponseDto
{
    /// <summary>Booking ID.</summary>
    public int Id { get; set; }

    /// <summary>Unique booking reference code.</summary>
    public string BookingReference { get; set; } = null!;

    /// <summary>Flight ID for this booking.</summary>
    public int FlightId { get; set; }

    /// <summary>Flight number.</summary>
    public string FlightNumber { get; set; } = null!;

    /// <summary>User ID who made the booking.</summary>
    public int UserId { get; set; }

    /// <summary>Number of passengers.</summary>
    public int PassengerCount { get; set; }

    /// <summary>Total price of the booking.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Current status of the booking.</summary>
    public string Status { get; set; } = null!;

    /// <summary>When the booking was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the booking was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Passengers in this booking.</summary>
    public List<PassengerResponseDto> Passengers { get; set; } = new();

    /// <summary>Special notes or requests.</summary>
    public string? Notes { get; set; }
}
