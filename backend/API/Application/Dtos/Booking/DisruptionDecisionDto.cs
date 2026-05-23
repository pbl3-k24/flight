namespace API.Application.Dtos.Booking;

public class FlightDisruptionOptionResponse
{
    public int DecisionId { get; set; }
    public int BookingId { get; set; }
    public int AffectedFlightId { get; set; }
    public int LegType { get; set; }
    public DateTime DecisionDeadline { get; set; }
    public string Status { get; set; } = "PENDING";
    public List<DisruptionFlightOptionDto> FlightOptions { get; set; } = [];
}

public class DisruptionFlightOptionDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = null!;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int AvailableSeats { get; set; }
}

public class RebookDisruptionDecisionDto
{
    public int DecisionId { get; set; }
    public int NewFlightId { get; set; }
}
