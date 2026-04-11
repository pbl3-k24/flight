namespace FlightBooking.Application.DTOs.Payment;

public record PaymentDto(
    Guid Id,
    Guid BookingId,
    string Gateway,
    decimal Amount,
    string Currency,
    string Status,
    string? TransactionRef,
    string? GatewayTransactionId,
    DateTime CreatedAt);

public record PaymentInitiationDto(
    Guid PaymentId,
    string PaymentUrl,
    string TransactionRef,
    DateTime ExpiresAt);

public record RefundRequest(
    Guid BookingItemId,
    string Reason,
    bool IsFullRefund = true,
    decimal? PartialAmount = null);

public record RefundDto(
    Guid Id,
    Guid PaymentId,
    Guid? BookingItemId,
    decimal Amount,
    string Reason,
    string Status,
    string? GatewayRef,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record RefundEligibilityDto(
    bool IsEligible,
    decimal RefundableAmount,
    decimal CancellationFee,
    string? IneligibilityReason);

public record ReconciliationReportDto(
    DateOnly Date,
    string Gateway,
    int TotalTransactions,
    decimal TotalAmount,
    int MatchedCount,
    int DiscrepancyCount);

public record ReconciliationDiscrepancyDto(
    Guid PaymentId,
    string TransactionRef,
    decimal SystemAmount,
    decimal? GatewayAmount,
    string DiscrepancyType);
