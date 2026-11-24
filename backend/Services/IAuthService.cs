using System.Security.Claims;
using AdvancedOrderSystem.Models.DTOs.Auth;

namespace AdvancedOrderSystem.Services;

public interface IAuthService
{
    // ---- Sign up and sign in ----

    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    Task<RegisterResponse> RegisterCustomerAsync(CustomerRegisterRequest request);

    // Signs the user in unless two-factor is required
    Task<LoginResponse> LoginAsync(LoginRequest request);

    Task<AuthUserResponse> VerifyTwoFactorAsync(TwoFactorLoginRequest request);

    Task LogoutAsync();

    Task<AuthUserResponse?> GetCurrentUserAsync(ClaimsPrincipal principal);

    // ---- Email confirmation ----

    Task ConfirmEmailAsync(ConfirmEmailRequest request);

    Task ResendConfirmationAsync(EmailRequest request);

    // ---- Passwords ----

    Task ForgotPasswordAsync(EmailRequest request);

    Task ResetPasswordAsync(ResetPasswordRequest request);

    Task ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request);

    // ---- Two-factor authentication ----

    Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync(ClaimsPrincipal principal);

    Task<RecoveryCodesResponse> EnableTwoFactorAsync(
        ClaimsPrincipal principal, TwoFactorCodeRequest request);

    Task DisableTwoFactorAsync(ClaimsPrincipal principal, PasswordRequest request);

    Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal, PasswordRequest request);
}
