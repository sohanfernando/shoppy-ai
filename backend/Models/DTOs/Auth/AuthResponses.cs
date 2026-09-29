namespace AdvancedOrderSystem.Models.DTOs.Auth;

public class AuthUserResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // "Admin" or "Customer"
    public string Role { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    // True when the account signs in with Google instead of a password
    public bool HasPassword { get; set; } = true;

    public bool TwoFactorEnabled { get; set; }
}

public class LoginResponse
{
    // true = the password was correct but a 2FA code is still needed
    public bool RequiresTwoFactor { get; set; }

    public AuthUserResponse? User { get; set; }
}

public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;

    // false = the account still needs its email confirmed before signing in
    public bool CanSignIn { get; set; }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;

    public MessageResponse()
    {
    }

    public MessageResponse(string message)
    {
        Message = message;
    }
}

public class TwoFactorSetupResponse
{
    // The secret, formatted in groups so it can be typed by hand
    public string SharedKey { get; set; } = string.Empty;

    // otpauth:// URI the frontend turns into a QR code
    public string AuthenticatorUri { get; set; } = string.Empty;
}

public class RecoveryCodesResponse
{
    public List<string> RecoveryCodes { get; set; } = new();
}
