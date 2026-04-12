namespace API.Domain.Enums;

/// <summary>
/// Represents the type of payment method used for a booking.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Payment via credit card.</summary>
    CreditCard = 1,

    /// <summary>Payment via debit card.</summary>
    DebitCard = 2,

    /// <summary>Payment via digital wallet (e.g., PayPal, Google Pay).</summary>
    DigitalWallet = 3,

    /// <summary>Payment via bank transfer.</summary>
    BankTransfer = 4
}
