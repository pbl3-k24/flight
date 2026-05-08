namespace API.Application.Dtos.AdditionalService;

public class AdditionalServiceDto
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

public class SeatClassServiceConfigDto
{
    public int SeatClassId { get; set; }
    public List<AdditionalServiceDto> IncludedServices { get; set; } = [];
    public List<AdditionalServiceDto> OptionalServices { get; set; } = [];
}
