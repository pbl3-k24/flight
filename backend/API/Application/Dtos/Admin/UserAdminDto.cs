namespace API.Application.Dtos.Admin;

public class UserManagementResponse
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public int Status { get; set; } // 0=Active, 1=Inactive, 2=Suspended

    public string StatusName { get; set; } = null!;

    public List<string> Roles { get; set; } = [];

    public int BookingCount { get; set; }

    public decimal TotalSpent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }
}

public class UpdateUserStatusDto
{
    public int Status { get; set; } // 0=Active, 1=Inactive, 2=Suspended

    public string? Reason { get; set; }
}

public class AssignRoleDto
{
    public int RoleId { get; set; }
}

public class PromotionManagementResponse
{
    public int PromotionId { get; set; }

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int DiscountType { get; set; } // 0=PERCENTAGE, 1=FIXED

    public decimal DiscountValue { get; set; }

    public decimal MinimumAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsageCount { get; set; }

    public bool IsActive { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreatePromotionDto
{
    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int DiscountType { get; set; } // 0=PERCENTAGE, 1=FIXED

    public decimal DiscountValue { get; set; }

    public decimal MinimumAmount { get; set; } = 0;

    public int? UsageLimit { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }
}

public class UpdatePromotionDto
{
    public string? Description { get; set; }

    public decimal? DiscountValue { get; set; }

    public int? UsageLimit { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool? IsActive { get; set; }
}
