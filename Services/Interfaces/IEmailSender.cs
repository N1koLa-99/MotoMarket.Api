namespace MotoMarket.Api.Services.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody = null);
}