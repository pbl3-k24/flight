namespace API.Application.Dtos.TicketUpgrade;

using API.Application.Dtos.Payment;

public class TicketUpgradeQuoteRequestDto
{
    public int ToSeatClassId { get; set; }
}

public class TicketUpgradeQuoteResponseDto
{
    public int BookingId { get; set; }
    public int TicketId { get; set; }
    public int FromSeatClassId { get; set; }
    public int ToSeatClassId { get; set; }
    public decimal PaidAmountOfOldTicket { get; set; }
    public decimal NewTicketAmount { get; set; }
    public decimal FareDifference { get; set; } // NewTicketAmount - PaidAmountOfOldTicket
    public decimal UpgradeFee { get; set; }
    public decimal UpgradeAmount { get; set; } // NewTicketAmount - PaidAmountOfOldTicket + UpgradeFee
    // Backward-compatible fields for FE still using legacy names.
    public decimal CurrentTicketPrice { get; set; }
    public decimal NewClassPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public string Currency { get; set; } = "VND";
}

public class CreateTicketUpgradeRequestDto
{
    public int ToSeatClassId { get; set; }
}

public class TicketUpgradeRequestResponseDto
{
    public int RequestId { get; set; }
    public int BookingId { get; set; }
    public int TicketId { get; set; }
    public int FromSeatClassId { get; set; }
    public int ToSeatClassId { get; set; }
    public decimal PriceDifference { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class InitiateTicketUpgradePaymentDto
{
    public string PaymentMethod { get; set; } = "VNPAY";
}

public class TicketUpgradePaymentResponseDto
{
    public int RequestId { get; set; }
    public PaymentResponse Payment { get; set; } = null!;
}
