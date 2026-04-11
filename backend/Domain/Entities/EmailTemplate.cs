namespace FlightBooking.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty; // otp_verification, booking_confirmed, flight_changed, refund_processed
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
