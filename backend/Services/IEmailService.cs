namespace AdvancedOrderSystem.Services;

public interface IEmailService
{
    Task SendAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}
