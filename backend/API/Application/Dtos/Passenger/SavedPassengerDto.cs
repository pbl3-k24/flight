namespace API.Application.Dtos.Passenger;

public class SavedPassengerResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? DocumentNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsDefaultOwner { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSavedPassengerDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? DocumentNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}

public class UpdateSavedPassengerDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? DocumentNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}

