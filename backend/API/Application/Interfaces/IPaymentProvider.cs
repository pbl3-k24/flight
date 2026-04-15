namespace API.Application.Interfaces;

public class PaymentProviderRequest
{
    public decimal Amount { get; set; }

    public int BookingId { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? ReturnUrl { get; set; }

    public string? OrderDescription { get; set; }
}

public class PaymentProviderResponse
{
    public string PaymentLink { get; set; } = null!;

    public string? QrCode { get; set; }

    public string TransactionId { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
}

public interface IPaymentProvider
{
    /// <summary>
    /// Generates a payment link for the provider.
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment link and QR code</returns>
    Task<PaymentProviderResponse> GeneratePaymentLinkAsync(PaymentProviderRequest request);

    /// <summary>
    /// Verifies the callback signature from the provider.
    /// </summary>
    /// <param name="signature">Signature from callback</param>
    /// <param name="data">Data used to generate signature</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifyCallbackSignatureAsync(string signature, string data);

    /// <summary>
    /// Gets payment status from provider.
    /// </summary>
    /// <param name="transactionId">Transaction ID from provider</param>
    /// <returns>Payment status</returns>
    Task<string> GetPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Refunds a payment with the provider.
    /// </summary>
    /// <param name="transactionId">Transaction ID to refund</param>
    /// <param name="amount">Refund amount</param>
    /// <returns>Success indicator</returns>
    Task<bool> RefundAsync(string transactionId, decimal amount);
}
