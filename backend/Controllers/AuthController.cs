using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Auth;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdvancedOrderSystem.Controllers;

// Errors are turned into status codes by GlobalExceptionHandler
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public AuthController(
        IAuthService authService,
        IAuthenticationSchemeProvider schemeProvider)
    {
        _authService = authService;
        _schemeProvider = schemeProvider;
    }

    // ==================================
    // Sign up and sign in
    // ==================================

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        return Ok(await _authService.RegisterAsync(request));
    }

    // Customer sign-up: no registration key
    [HttpPost("register-customer")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<RegisterResponse>> RegisterCustomer(
        CustomerRegisterRequest request)
    {
        return Ok(await _authService.RegisterCustomerAsync(request));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }

    // ==================================
    // Google sign-in (customers only)
    // ==================================

    [HttpGet("google/start")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleStart([FromQuery] string? returnUrl)
    {
        // Google credentials are optional, so say so instead of failing with a 500
        if (await _schemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) == null)
        {
            return Redirect(
                _authService.BuildUnavailableRedirect(
                    "Google sign-in is not set up on this server yet."));
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl })
            ?? "/api/auth/google/callback";

        return Challenge(
            _authService.BuildGoogleChallenge(redirectUrl),
            GoogleDefaults.AuthenticationScheme);
    }

    // Google sends the browser back here, and we send it on to the frontend
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl)
    {
        return Redirect(await _authService.CompleteGoogleSignInAsync(returnUrl));
    }

    // Second step when two-factor authentication is enabled
    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<AuthUserResponse>> VerifyTwoFactor(
        TwoFactorLoginRequest request)
    {
        return Ok(await _authService.VerifyTwoFactorAsync(request));
    }

    // Anonymous so an already-expired session can still be cleared
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();

        return NoContent();
    }

    // The frontend calls this on start-up to restore the session
    [HttpGet("me")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<AuthUserResponse>> Me()
    {
        var user = await _authService.GetCurrentUserAsync(User);

        // The account was removed after the cookie was issued
        if (user == null)
        {
            await _authService.LogoutAsync();

            return Unauthorized();
        }

        return Ok(user);
    }

    // ==================================
    // Email confirmation
    // ==================================

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<MessageResponse>> ConfirmEmail(ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request);

        return Ok(new MessageResponse("Your email is confirmed. You can sign in now."));
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<MessageResponse>> ResendConfirmation(EmailRequest request)
    {
        await _authService.ResendConfirmationAsync(request);

        return Ok(new MessageResponse(
            "If that email needs confirming, a new link is on its way."));
    }

    // ==================================
    // Passwords
    // ==================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(EmailRequest request)
    {
        await _authService.ForgotPasswordAsync(request);

        return Ok(new MessageResponse(
            "If an account exists for that email, a reset link is on its way."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.AuthRateLimitPolicy)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);

        return Ok(new MessageResponse(
            "Your password has been reset. You can sign in now."));
    }

    [HttpPost("change-password")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<MessageResponse>> ChangePassword(ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(User, request);

        return Ok(new MessageResponse(
            "Your password has been changed. Other devices have been signed out."));
    }

    // ==================================
    // Two-factor authentication
    // ==================================

    [HttpGet("2fa/setup")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<TwoFactorSetupResponse>> StartTwoFactorSetup()
    {
        return Ok(await _authService.StartTwoFactorSetupAsync(User));
    }

    [HttpPost("2fa/enable")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<RecoveryCodesResponse>> EnableTwoFactor(
        TwoFactorCodeRequest request)
    {
        return Ok(await _authService.EnableTwoFactorAsync(User, request));
    }

    [HttpPost("2fa/disable")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<MessageResponse>> DisableTwoFactor(PasswordRequest request)
    {
        await _authService.DisableTwoFactorAsync(User, request);

        return Ok(new MessageResponse("Two-factor authentication is turned off."));
    }

    [HttpPost("2fa/recovery-codes")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<RecoveryCodesResponse>> RegenerateRecoveryCodes(
        PasswordRequest request)
    {
        return Ok(await _authService.RegenerateRecoveryCodesAsync(User, request));
    }
}
