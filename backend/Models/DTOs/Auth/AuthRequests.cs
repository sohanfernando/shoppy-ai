using AdvancedOrderSystem.Auth;

namespace AdvancedOrderSystem.Models.DTOs.Auth;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public string RegistrationKey { get; set; } = string.Empty;
}

// Customers sign up without a key; their account gets the Customer role
public class CustomerRegisterRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // true = stay signed in for 7 days
    public bool RememberMe { get; set; }

    // Which sign-in page this came from: "Customer" or "Admin".
    // An account may only sign in through its own page.
    public string Portal { get; set; } = AuthConstants.CustomerRole;
}

public class TwoFactorLoginRequest
{
    public string Code { get; set; } = string.Empty;

    // true = the code is a recovery code instead of an authenticator code
    public bool IsRecoveryCode { get; set; }

    public bool RememberMe { get; set; }

    // true = don't ask for a code on this device for 30 days
    public bool RememberMachine { get; set; }
}

public class ConfirmEmailRequest
{
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;
}

public class EmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}

public class TwoFactorCodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class PasswordRequest
{
    public string Password { get; set; } = string.Empty;
}
