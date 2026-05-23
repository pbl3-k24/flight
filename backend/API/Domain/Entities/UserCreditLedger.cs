namespace API.Domain.Entities;

public class UserCreditLedger
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "VND";
    public string Reason { get; set; } = null!;
    public string ReferenceType { get; set; } = null!;
    public int ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual User User { get; set; } = null!;
}

