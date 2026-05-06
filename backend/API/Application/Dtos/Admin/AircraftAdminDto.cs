namespace API.Application.Dtos.Admin;

using System.ComponentModel.DataAnnotations;

public class CreateAircraftDto
{
    [Required(ErrorMessage = "Registration number is required")]
    [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters")]
    public string RegistrationNumber { get; set; } = null!;

    [Required(ErrorMessage = "Model is required")]
    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    public string Model { get; set; } = null!;

    [Required(ErrorMessage = "Total seats is required")]
    [Range(1, 1000, ErrorMessage = "Total seats must be between 1 and 1000")]
    public int TotalSeats { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Seat configuration for each class
    /// </summary>
    public List<CreateAircraftSeatTemplateDto> SeatTemplates { get; set; } = new();
}

public class CreateAircraftSeatTemplateDto
{
    [Required(ErrorMessage = "Seat class ID is required")]
    public int SeatClassId { get; set; }

    [Required(ErrorMessage = "Default seat count is required")]
    [Range(1, 1000, ErrorMessage = "Seat count must be between 1 and 1000")]
    public int DefaultSeatCount { get; set; }

    [Required(ErrorMessage = "Default base price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal DefaultBasePrice { get; set; }
}

public class UpdateAircraftDto
{
    [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters")]
    public string? RegistrationNumber { get; set; }

    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    public string? Model { get; set; }

    [Range(1, 1000, ErrorMessage = "Total seats must be between 1 and 1000")]
    public int? TotalSeats { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// If provided, will replace all existing seat templates
    /// </summary>
    public List<CreateAircraftSeatTemplateDto>? SeatTemplates { get; set; }
}

public class AircraftManagementResponse
{
    public int AircraftId { get; set; }
    public string RegistrationNumber { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int TotalSeats { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int ActiveFlightsCount { get; set; }
    public List<AircraftSeatTemplateResponse> SeatTemplates { get; set; } = new();
}

public class AircraftSeatTemplateResponse
{
    public int Id { get; set; }
    public int SeatClassId { get; set; }
    public string SeatClassName { get; set; } = null!;
    public int DefaultSeatCount { get; set; }
    public decimal DefaultBasePrice { get; set; }
    public bool IsDeleted { get; set; }
}
