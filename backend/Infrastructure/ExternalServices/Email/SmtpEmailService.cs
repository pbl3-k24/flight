using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlightBooking.Infrastructure.ExternalServices.Email;

public class SmtpEmailService(
    IEmailTemplateRepository templateRepository,
    IConfiguration config) : IEmailService
{
    public async Task SendAsync(string to, string templateKey, object templateData)
    {
        var template = await templateRepository.GetByKeyAsync(templateKey);
        if (template is null || !template.IsActive)
            throw new InvalidOperationException($"Email template '{templateKey}' not found or inactive.");

        var subject = RenderTemplate(template.Subject, templateData);
        var htmlBody = RenderTemplate(template.HtmlBody, templateData);

        await SendRawAsync(to, subject, htmlBody);
    }

    public async Task SendRawAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            config["Email:FromName"] ?? "Flight Booking",
            config["Email:FromAddress"] ?? "noreply@flightbooking.vn"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        var host = config["Email:SmtpHost"] ?? "localhost";
        var port = int.TryParse(config["Email:SmtpPort"], out var p) ? p : 1025;
        var useSsl = bool.TryParse(config["Email:UseSsl"], out var ssl) && ssl;

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

        var smtpUser = config["Email:SmtpUser"];
        var smtpPass = config["Email:SmtpPassword"];
        if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            await client.AuthenticateAsync(smtpUser, smtpPass);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public Task<bool> IsDeliverableAsync(string email)
    {
        // Basic format check; production would use a third-party validation API
        var isValid = Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return Task.FromResult(isValid);
    }

    private static string RenderTemplate(string template, object data)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(data)) ?? [];

        foreach (var (key, value) in dict)
            template = template.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

        return template;
    }
}
