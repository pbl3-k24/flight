namespace API.Application.Interfaces;

using API.Application.Dtos.TicketUpgrade;

public interface ITicketUpgradeService
{
    Task<bool> IsUpgradePaymentAsync(int paymentId);
    Task<TicketUpgradeQuoteResponseDto> GetQuoteAsync(int bookingId, int ticketId, int toSeatClassId, int userId);
    Task<TicketUpgradeRequestResponseDto> CreateRequestAsync(int bookingId, int ticketId, int toSeatClassId, int userId);
    Task<TicketUpgradePaymentResponseDto> InitiatePaymentAsync(int requestId, string paymentMethod, int userId);
    Task<bool> ProcessUpgradePaymentAsync(int paymentId, string paymentStatus);
    Task<int> ExpirePendingRequestsAsync();
}
