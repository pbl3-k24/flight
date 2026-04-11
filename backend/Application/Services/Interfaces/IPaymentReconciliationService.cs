using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface IPaymentReconciliationService
{
    /// <summary>Reconcile completed payments against gateway statements for a given date.</summary>
    Task<ReconciliationReportDto> ReconcileAsync(DateOnly date, string gateway);

    /// <summary>List discrepancies found in reconciliation (called by admin or scheduler).</summary>
    Task<IEnumerable<ReconciliationDiscrepancyDto>> GetDiscrepanciesAsync(DateOnly date, string? gateway = null);
}
