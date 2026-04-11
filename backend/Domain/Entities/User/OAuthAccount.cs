using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.User;

public class OAuthAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public OAuthProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }

    public User User { get; set; } = null!;
}
