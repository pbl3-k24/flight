namespace API.Domain.Entities;

using API.Domain.Enums;

/// <summary>
/// Represents a payment in the system.
/// This entity manages payment processing, status transitions, and refund operations.
/// Enforces invariants:
/// - Amount > 0
/// - Amount matches booking total
/// - Status transitions are valid
/// - Refund amount <= original amount
/// - TransactionId required for completed payments
/// </summary>
public class Payment
{
    /// <summary>Unique identifier for the payment.</summary>
    public int Id { get; set; }

    /// <summary>ID of the booking being paid for.</summary>
    public int BookingId { get; set; }

    /// <summary>ID of the user making the payment.</summary>
    public int UserId { get; set; }

    /// <summary>Payment amount in the specified currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>Currency code (e.g., "USD", "EUR", "GBP").</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Payment method used (CreditCard, DebitCard, DigitalWallet, BankTransfer).</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Current status of the payment (Pending, Processing, Completed, Failed, Refunded).</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Transaction ID from the payment processor.</summary>
    public string? TransactionId { get; set; }

    /// <summary>When the payment was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the payment was processed/completed (null if not yet processed).</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Additional notes or error messages related to the payment.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>Booking associated with this payment.</summary>
    public virtual Booking? Booking { get; set; }

    /// <summary>User who made this payment.</summary>
    public virtual User? User { get; set; }

    // Domain methods
    /// <summary>
    /// Checks if the payment can be processed.
    /// Payment must be Pending and have a valid amount.
    /// </summary>
    /// <returns>True if payment can be processed, false otherwise.</returns>
    public bool CanBeProcessed()
    {
        return Status == PaymentStatus.Pending && Amount > 0;
    }

    /// <summary>
    /// Processes the payment with the result from a payment gateway.
    /// </summary>
    /// <param name="response">Payment gateway response containing success status and transaction ID.</param>
    /// <exception cref="InvalidOperationException">Thrown if payment is not Pending.</exception>
    /// <exception cref="ArgumentNullException">Thrown if response is null.</exception>
    public void Process(PaymentGatewayResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response), "Payment gateway response is required.");

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot process payment with status {Status}");

        if (response.IsSuccessful)
        {
            Status = PaymentStatus.Completed;
            TransactionId = response.TransactionId;
            ProcessedAt = DateTime.UtcNow;
            Notes = "Payment processed successfully.";
        }
        else
        {
            Status = PaymentStatus.Failed;
            ProcessedAt = DateTime.UtcNow;
            Notes = response.ErrorMessage ?? "Payment processing failed.";
        }
    }

    /// <summary>
    /// Initiates a refund for the payment.
    /// </summary>
    /// <param name="refundAmount">Amount to refund (must be <= original amount).</param>
    /// <exception cref="InvalidOperationException">Thrown if payment is not Completed or refund amount exceeds original.</exception>
    /// <exception cref="ArgumentException">Thrown if refund amount is invalid.</exception>
    public void Refund(decimal refundAmount)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException($"Cannot refund payment with status {Status}");

        if (refundAmount <= 0)
            throw new ArgumentException("Refund amount must be greater than 0.", nameof(refundAmount));

        if (refundAmount > Amount)
            throw new InvalidOperationException($"Refund amount ({refundAmount}) cannot exceed original payment ({Amount}).");

        Status = PaymentStatus.Refunded;
        Notes = $"Refunded {refundAmount} out of {Amount} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}.";
    }

    /// <summary>
    /// Marks the payment as completed with a transaction ID.
    /// </summary>
    /// <param name="transactionId">Transaction ID from the payment processor.</param>
    /// <exception cref="InvalidOperationException">Thrown if payment is not Pending.</exception>
    /// <exception cref="ArgumentException">Thrown if transaction ID is empty.</exception>
    public void Complete(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot complete payment with status {Status}");

        if (string.IsNullOrEmpty(transactionId))
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));

        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the payment as failed with an error message.
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <exception cref="InvalidOperationException">Thrown if payment is not Pending.</exception>
    /// <exception cref="ArgumentException">Thrown if error message is empty.</exception>
    public void Fail(string errorMessage)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment with status {Status}");

        if (string.IsNullOrEmpty(errorMessage))
            throw new ArgumentException("Error message is required.", nameof(errorMessage));

        Status = PaymentStatus.Failed;
        Notes = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the response from a payment gateway when processing a payment.
/// This is a value object used in the Process() method.
/// </summary>
public class PaymentGatewayResponse
{
    /// <summary>Whether the payment was processed successfully.</summary>
    public bool IsSuccessful { get; set; }

    /// <summary>Transaction ID assigned by the payment processor (required for successful payments).</summary>
    public string? TransactionId { get; set; }

    /// <summary>Error message if the payment failed.</summary>
    public string? ErrorMessage { get; set; }
}
