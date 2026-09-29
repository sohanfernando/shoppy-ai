namespace AdvancedOrderSystem.Services.Impl;

// Development fallback: writes the email to the log instead of sending it,
// so confirmation and reset links can be copied from the terminal
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email not sent (no SMTP configured).\nTo: {ToAddress}\nSubject: {Subject}\n{Body}",
            toAddress,
            subject,
            textBody);

        return Task.CompletedTask;
    }
}
