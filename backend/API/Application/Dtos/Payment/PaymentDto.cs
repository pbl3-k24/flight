namespace API.Application.Dtos.Payment;

public class InitiatePaymentDto
{
    public int BookingId { get; set; }

    public string PaymentMethod { get; set; } = null!; // CARD, BANK, WALLET, MOMO, VNPAY, PAYPAL

    public string? PromoCode { get; set; }
}

public class PaymentResponse
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public string Status { get; set; } = null!; // Pending, Completed, Failed, Refunded

    public decimal Amount { get; set; }

    public string Provider { get; set; } = null!;

    public string? TransactionRef { get; set; }

    public string? PaymentLink { get; set; }

    public string? QrCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class PaymentCallbackDto
{
    public string TransactionId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Signature { get; set; }

    public Dictionary<string, string>? AdditionalData { get; set; }

    public string? RawData { get; set; } // Raw payload for signature verification
}

public class PaymentHistoryResponse
{
    public int PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}
