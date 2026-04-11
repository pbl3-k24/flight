using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class RefundService(
    IRefundRepository refundRepository,
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IRefundPolicyService refundPolicyService,
    INotificationService notificationService,
    IAuditLogService auditLogService,
    IWalletLedgerRepository ledgerRepository) : IRefundService
{
    public async Task<RefundDto> GetByIdAsync(Guid id)
    {
        var refund = await refundRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Refund {id} not found.");
        return MapToDto(refund);
    }

    public async Task<IEnumerable<RefundDto>> GetByPaymentAsync(Guid paymentId)
    {
        var refunds = await refundRepository.GetByPaymentAsync(paymentId);
        return refunds.Select(MapToDto);
    }

    public async Task<IEnumerable<RefundDto>> GetPendingAsync()
    {
        var refunds = await refundRepository.GetByStatusAsync("pending");
        return refunds.Select(MapToDto);
    }

    public async Task<RefundDto> RequestRefundAsync(RefundRequest request, Guid requestedBy)
    {
        var eligibility = await refundPolicyService.EvaluateEligibilityAsync(request.BookingItemId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException($"Refund not eligible: {eligibility.IneligibilityReason}");

        var bookingItem = await bookingRepository.GetItemByIdAsync(request.BookingItemId)
            ?? throw new KeyNotFoundException($"Booking item {request.BookingItemId} not found.");

        var payment = await paymentRepository.GetCompletedByBookingAsync(bookingItem.BookingId)
            ?? throw new InvalidOperationException("No completed payment found for this booking.");

        var refundAmount = request.IsFullRefund
            ? eligibility.RefundableAmount
            : Math.Min(request.PartialAmount ?? eligibility.RefundableAmount, eligibility.RefundableAmount);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            BookingItemId = request.BookingItemId,
            Amount = refundAmount,
            Reason = request.Reason,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await refundRepository.AddAsync(refund);
        await refundRepository.SaveChangesAsync();
        return MapToDto(refund);
    }

    public async Task<RefundDto> ApproveRefundAsync(Guid refundId, Guid adminId)
    {
        var refund = await refundRepository.GetByIdAsync(refundId)
            ?? throw new KeyNotFoundException($"Refund {refundId} not found.");

        if (refund.Status != "pending")
            throw new InvalidOperationException($"Refund is in status '{refund.Status}' and cannot be approved.");

        refund.Status = "processing";
        refund.ProcessedBy = adminId;
        refund.UpdatedAt = DateTime.UtcNow;
        await refundRepository.SaveChangesAsync();

        // In real implementation: call gateway refund API here
        // For now simulate async processing
        await auditLogService.LogAsync("refund_approved", "Refund", refundId.ToString(),
            new { Status = "pending" }, new { Status = "processing" }, adminId);

        return MapToDto(refund);
    }

    public async Task<RefundDto> RejectRefundAsync(Guid refundId, string reason, Guid adminId)
    {
        var refund = await refundRepository.GetByIdAsync(refundId)
            ?? throw new KeyNotFoundException($"Refund {refundId} not found.");

        if (refund.Status != "pending")
            throw new InvalidOperationException($"Refund is in status '{refund.Status}' and cannot be rejected.");

        refund.Status = "rejected";
        refund.UpdatedAt = DateTime.UtcNow;
        await refundRepository.SaveChangesAsync();

        await auditLogService.LogAsync("refund_rejected", "Refund", refundId.ToString(),
            new { Status = "pending" }, new { Status = "rejected", Reason = reason }, adminId);

        await notificationService.SendRefundProcessedAsync(refundId);
        return MapToDto(refund);
    }

    public async Task HandleRefundCallbackAsync(Guid refundId, bool success, string? gatewayRef)
    {
        var refund = await refundRepository.GetByIdAsync(refundId)
            ?? throw new KeyNotFoundException($"Refund {refundId} not found.");

        refund.Status = success ? "completed" : "failed";
        refund.GatewayRef = gatewayRef;
        refund.UpdatedAt = DateTime.UtcNow;
        await refundRepository.SaveChangesAsync();

        if (success)
        {
            // Write immutable ledger entry
            await ledgerRepository.AddAsync(new WalletLedger
            {
                Id = Guid.NewGuid(),
                PaymentId = refund.PaymentId,
                RefundId = refund.Id,
                EntryType = "credit",
                Amount = refund.Amount,
                Currency = "VND",
                Description = $"Refund for payment {refund.PaymentId}",
                CreatedAt = DateTime.UtcNow
            });
            await ledgerRepository.SaveChangesAsync();
        }

        await notificationService.SendRefundProcessedAsync(refundId);
    }

    private static RefundDto MapToDto(Refund r) =>
        new(r.Id, r.PaymentId, r.BookingItemId, r.Amount, r.Reason, r.Status, r.GatewayRef, r.CreatedAt, r.UpdatedAt);
}
