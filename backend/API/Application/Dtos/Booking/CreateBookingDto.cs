namespace API.Application.Dtos.Booking;

public class CreateBookingDto
{
    public int OutboundFlightId { get; set; }

    public string? OutboundFlightNumber { get; set; }

    public DateTime? OutboundDepartureDate { get; set; }

    public int? ReturnFlightId { get; set; }

    public string? ReturnFlightNumber { get; set; }

    public DateTime? ReturnDepartureDate { get; set; }

    public int PassengerCount { get; set; }

    public int SeatClassId { get; set; }

    public List<CreatePassengerDto> Passengers { get; set; } = [];

    public string? PromotionCode { get; set; }

    public int? PromotionId { get; set; }

    public string? ContactEmail { get; set; }
}

public class CreatePassengerDto
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public string Nationality { get; set; } = null!;

    public string PassportNumber { get; set; } = null!;

    public List<PassengerServiceDto>? OptionalServices { get; set; }
}

public class PassengerServiceDto
{
    public int AdditionalServiceId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateBookingDto
{
    public List<UpdatePassengerDto>? Passengers { get; set; }
}

public class UpdatePassengerDto
{
    public int PassengerId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PassportNumber { get; set; } = null!;
}

public class BookingResponse
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public int? PromotionId { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public FlightBookingDetail OutboundFlight { get; set; } = null!;

    public FlightBookingDetail? ReturnFlight { get; set; }

    public List<PassengerDetail> Passengers { get; set; } = [];
}

public class FlightBookingDetail
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string DepartureAirport { get; set; } = null!;

    public string ArrivalAirport { get; set; } = null!;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public string SeatClass { get; set; } = null!;

    public decimal Price { get; set; }
}

public class PassengerDetail
{
    public int PassengerId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PassportNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public List<BookingServiceDetail> Services { get; set; } = [];
}

public class BookingServiceDetail
{
    public int AdditionalServiceId { get; set; }
    public string ServiceName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
