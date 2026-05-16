namespace API.Application.Dtos.Booking;

public class AddPassengerServiceDto
{
    public int AdditionalServiceId { get; set; }

    public int Quantity { get; set; }
}

public class UpdatePassengerServiceDto
{
    public int Quantity { get; set; }
}

public class PassengerServiceResponse
{
    public int BookingServiceId { get; set; }

    public int BookingPassengerId { get; set; }

    public int AdditionalServiceId { get; set; }

    public string ServiceName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public bool IsIncluded { get; set; }
}
