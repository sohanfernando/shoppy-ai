using AdvancedOrderSystem.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AdvancedOrderSystem.Services.Impl;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            // Port 465 is implicit TLS; everything else (587) upgrades with STARTTLS
            var secureSocketOptions = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                _options.Host, _options.Port, secureSocketOptions, cancellationToken);

            await client.AuthenticateAsync(
                _options.User, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "Sent \"{Subject}\" to {ToAddress}", subject, toAddress);
        }
        catch (Exception ex)
        {
            // A failed email must not fail the request that triggered it
            _logger.LogError(
                ex, "Could not send \"{Subject}\" to {ToAddress}", subject, toAddress);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
