using Domain.Common;

namespace Domain.Entities.Notification;

public class EmailTemplate : BaseEntity
{
    public string TemplateKey { get; set; } = string.Empty;  // e.g. "booking_confirmed"
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public bool IsActive { get; set; } = true;
}
