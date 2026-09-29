namespace AdvancedOrderSystem.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    // Required to create an admin account. Registration is disabled while empty.
    public string RegistrationKey { get; set; } = string.Empty;

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    // The very first admin is confirmed automatically, so email problems cannot lock you out
    public bool AutoConfirmFirstAdmin { get; set; } = true;
}
