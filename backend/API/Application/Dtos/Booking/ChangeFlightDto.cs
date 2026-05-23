namespace API.Application.Dtos.Booking;

public class ChangeFlightOptionResponse
{
    public int BookingId { get; set; }
    public int LegType { get; set; } // 0=Outbound, 1=Return
    public int CurrentFlightId { get; set; }
    public string CurrentFlightNumber { get; set; } = null!;
    public DateTime CurrentDepartureTime { get; set; }
    public DateTime CurrentArrivalTime { get; set; }
    public List<ChangeFlightCandidateDto> Candidates { get; set; } = [];
}

public class ChangeFlightCandidateDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = null!;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int AvailableSeats { get; set; }
    public decimal UnitFare { get; set; }
}

public class ChangeFlightQuoteRequestDto
{
    public int LegType { get; set; } // 0=Outbound, 1=Return
    public int NewFlightId { get; set; }
}

public class ChangeFlightQuoteResponseDto
{
    public int BookingId { get; set; }
    public int LegType { get; set; }
    public int OldFlightId { get; set; }
    public int NewFlightId { get; set; }
    public decimal OldAmount { get; set; }
    public decimal NewAmount { get; set; }
    public decimal FareDifference { get; set; }
    public decimal ChangeFee { get; set; }
    public decimal NetAmount { get; set; } // >0 pay more, <0 create credit
    public string Currency { get; set; } = "VND";
}

public class ConfirmChangeFlightRequestDto
{
    public int LegType { get; set; } // 0=Outbound, 1=Return
    public int NewFlightId { get; set; }
}

public class ConfirmChangeFlightResponseDto
{
    public int ChangeRequestId { get; set; }
    public int BookingId { get; set; }
    public int LegType { get; set; }
    public int OldFlightId { get; set; }
    public int NewFlightId { get; set; }
    public decimal NetAmount { get; set; }
    public bool PaymentRequired { get; set; }
    public int? PaymentId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
    public decimal CreditAmount { get; set; }
    public string Status { get; set; } = null!;
}
