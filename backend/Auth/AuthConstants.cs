namespace AdvancedOrderSystem.Auth;

public static class AuthConstants
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";

    public static readonly string[] AllRoles = [AdminRole, CustomerRole];

    public const string AdminPolicy = "AdminOnly";
    public const string CustomerPolicy = "CustomerOnly";

    // Signed in as either role
    public const string SignedInPolicy = "SignedIn";

    // SignalR group that receives admin notifications
    public const string AdminNotificationGroup = "admins";

    public const string CookieName = "aos.auth";
    public const string TwoFactorUserIdCookieName = "aos.2fa";
    public const string TwoFactorRememberMeCookieName = "aos.2fa.remember";
    public const string ExternalCookieName = "aos.external";

    public const string AuthRateLimitPolicy = "auth";

    // Name of the token provider used for password reset links
    public const string PasswordResetTokenProvider = "PasswordReset";

    public static readonly TimeSpan RememberMeDuration = TimeSpan.FromDays(7);

    // Sliding lifetime of a normal (not remembered) session
    public static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);

    // How long the "enter your code" step stays open after the password is accepted
    public static readonly TimeSpan TwoFactorWindow = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan TwoFactorRememberMeDuration = TimeSpan.FromDays(30);

    public static readonly TimeSpan PasswordResetTokenLifespan = TimeSpan.FromHours(1);
}
