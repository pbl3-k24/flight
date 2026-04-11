using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.User;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public DateOnly? PassportExpiry { get; set; }
    public string? AvatarUrl { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
