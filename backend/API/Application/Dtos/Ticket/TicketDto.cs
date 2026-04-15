namespace API.Application.Dtos.Ticket;

public class TicketResponse
{
    public int TicketId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public int BookingId { get; set; }

    public int PassengerId { get; set; }

    public string PassengerName { get; set; } = null!;

    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string SeatNumber { get; set; } = null!;

    public string Status { get; set; } = null!; // Issued, CheckedIn, Used, Cancelled

    public DateTime IssuedAt { get; set; }

    public string? QrCode { get; set; }

    public DateTime DepartureTime { get; set; }

    public string DepartureAirport { get; set; } = null!;

    public string ArrivalAirport { get; set; } = null!;
}

public class TicketDownloadRequest
{
    public string Format { get; set; } = "pdf"; // pdf or html
}

public class ChangeTicketDto
{
    public int NewFlightId { get; set; }

    public string? NewSeatNumber { get; set; }

    public string Reason { get; set; } = null!;
}
