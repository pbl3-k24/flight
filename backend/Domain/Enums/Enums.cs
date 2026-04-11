namespace Domain.Enums;

public enum UserStatus
{
    Active,
    Inactive,
    Banned,
    PendingVerification
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum OAuthProvider
{
    Google
}

public enum FlightStatus
{
    Scheduled,
    CheckInOpen,
    Boarding,
    Departed,
    Arrived,
    Delayed,
    Cancelled
}

public enum FareClassCode
{
    Economy,
    Business
}

public enum PassengerType
{
    Adult,
    Child,
    Infant
}

public enum BookingStatus
{
    PendingPayment,
    Confirmed,
    Cancelled,
    Refunded,
    Expired
}

public enum TicketStatus
{
    Issued,
    Used,
    Voided,
    Refunded
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Refunded,
    PartiallyRefunded
}

public enum PaymentGateway
{
    VNPay,
    MoMo,
    ZaloPay
}

public enum PaymentEventType
{
    Created,
    CallbackReceived,
    Succeeded,
    Failed,
    RefundInitiated,
    RefundSucceeded,
    RefundFailed
}

public enum RefundStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Rejected
}

public enum LedgerEntryType
{
    Debit,
    Credit
}

public enum NotificationJobStatus
{
    Queued,
    Processing,
    Sent,
    Failed,
    Cancelled
}

public enum NotificationJobType
{
    EmailOtp,
    EmailBookingConfirmation,
    EmailTicketIssued,
    EmailFlightChange,
    EmailRefundStatus,
    EmailPasswordReset,
    EmailWelcome
}

public enum PriceSource
{
    Auto,
    ManualOverride
}

public enum OutboxEventStatus
{
    Pending,
    Processed,
    Failed
}
