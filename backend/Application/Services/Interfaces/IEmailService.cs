namespace FlightBooking.Application.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string templateKey, object templateData);
    Task SendRawAsync(string to, string subject, string htmlBody);
    Task<bool> IsDeliverableAsync(string email);
}
