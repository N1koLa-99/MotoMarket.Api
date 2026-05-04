using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MotoMarket.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IWebHostEnvironment _environment;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody = null)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Email Host липсва.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Email FromEmail липсва.");

        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("Email Username липсва.");

        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("Email Password липсва.");

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        if (_environment.IsDevelopment())
        {
            client.CheckCertificateRevocation = false;

            // Само локално. Това не трябва да ходи в production.
            client.ServerCertificateValidationCallback = AcceptCertificateInDevelopment;
        }

        var secureSocketOptions = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions);

        await client.AuthenticateAsync(_options.Username, _options.Password);

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    }

    private static bool AcceptCertificateInDevelopment(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}