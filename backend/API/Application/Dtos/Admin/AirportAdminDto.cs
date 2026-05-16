namespace API.Application.Dtos.Admin;

public class CreateAirportDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Province { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateAirportDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public bool? IsActive { get; set; }
}

public class AirportManagementResponse
{
    public int AirportId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Province { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
