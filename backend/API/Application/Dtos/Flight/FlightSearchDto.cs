namespace API.Application.Dtos.Flight;

public class FlightSearchDto
{
    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public int PassengerCount { get; set; }

    public int? SeatPreference { get; set; } // Nullable, can filter by seat class
}

public class FlightSearchResponse
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string DepartureAirport { get; set; } = null!;

    public string ArrivalAirport { get; set; } = null!;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int DurationMinutes { get; set; }

    public Dictionary<string, int> AvailableSeatsByClass { get; set; } = [];

    public Dictionary<string, decimal> PricesByClass { get; set; } = [];

    public string AirlineCode { get; set; } = null!;

    public string AircraftModel { get; set; } = null!;
}

public class FlightDetailResponse
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public string DepartureAirport { get; set; } = null!;

    public string ArrivalAirport { get; set; } = null!;

    public int DistanceKm { get; set; }

    public int DurationMinutes { get; set; }

    public string AircraftModel { get; set; } = null!;

    public Dictionary<string, SeatClassDetail> SeatInventory { get; set; } = [];
}

public class SeatClassDetail
{
    public int SeatClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int HeldSeats { get; set; }

    public int SoldSeats { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal BasePrice { get; set; }
}
