namespace FlightBooking.Application.Services.Interfaces;

public interface IPaymentWebhookService
{
    /// <summary>Process an inbound webhook from a payment gateway. Validates signature and idempotency.</summary>
    Task HandleWebhookAsync(string gateway, string rawPayload, IReadOnlyDictionary<string, string> headers);
}
