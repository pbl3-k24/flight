namespace API.Domain.Entities;

public class SavedPassenger
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? DocumentNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsDefaultOwner { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual User User { get; set; } = null!;
}

