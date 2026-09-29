namespace AdvancedOrderSystem.Auth;

public class GoogleOptions
{
    public const string SectionName = "Google";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public class EmailOptions
{
    public const string SectionName = "Email";

    // "Smtp" sends real email; anything else writes the message to the log
    public string Provider { get; set; } = "Console";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Advanced Order System";

    public bool IsSmtpConfigured =>
        string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(User) &&
        !string.IsNullOrWhiteSpace(Password);
}
