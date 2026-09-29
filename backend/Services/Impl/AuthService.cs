using System.Globalization;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Exceptions;
using AdvancedOrderSystem.Models.DTOs.Auth;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdvancedOrderSystem.Services.Impl;

public class AuthService : IAuthService
{
    private const int NameMaxLength = 150;
    private const int EmailMaxLength = 200;
    private const int RecoveryCodeCount = 10;

    private const string Issuer = "Advanced Order System";

    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string DuplicateEmailMessage = "An account with this email already exists.";
    private const string InvalidLinkMessage =
        "This link is invalid or has expired. Please request a new one.";
    private const string LockedOutMessage =
        "This account is temporarily locked because of too many failed sign-in attempts. Please try again later.";

    // Verified when the email is unknown, so a failed sign-in takes the same
    // time whether or not the account exists
    private static readonly string DummyPasswordHash =
        new PasswordHasher<AppUser>().HashPassword(new AppUser(), "not-a-real-password");

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailService emailService,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    // ========================================
    // Sign up
    // ========================================

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (!IsRegistrationKeyValid(request.RegistrationKey))
        {
            throw new ForbiddenException("Invalid registration key.");
        }

        var fullName = ValidateFullName(request.FullName);
        var email = ValidateEmail(request.Email);

        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow
        };

        ThrowIfFailed(await _userManager.CreateAsync(user, request.Password ?? string.Empty));

        ThrowIfFailed(await _userManager.AddToRoleAsync(user, AuthConstants.AdminRole));

        // The first admin is confirmed automatically, so a mail problem cannot lock everyone out
        var isFirstAdmin = await _userManager.Users.CountAsync() == 1;

        if (isFirstAdmin && _authOptions.AutoConfirmFirstAdmin)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation(
                "First admin account {UserId} registered and confirmed automatically", user.Id);

            return new RegisterResponse
            {
                Message = "Account created. You can sign in now.",
                CanSignIn = true
            };
        }

        await SendConfirmationEmailAsync(user);

        _logger.LogInformation("Admin account {UserId} registered", user.Id);

        return new RegisterResponse
        {
            Message = "Account created. Check your email for a confirmation link before signing in.",
            CanSignIn = false
        };
    }

    public async Task<RegisterResponse> RegisterCustomerAsync(CustomerRegisterRequest request)
    {
        var fullName = ValidateFullName(request.FullName);
        var email = ValidateEmail(request.Email);

        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow
        };

        ThrowIfFailed(await _userManager.CreateAsync(user, request.Password ?? string.Empty));
        ThrowIfFailed(await _userManager.AddToRoleAsync(user, AuthConstants.CustomerRole));

        await LinkCustomerProfileAsync(user);

        await SendConfirmationEmailAsync(user);

        _logger.LogInformation("Customer account {UserId} registered", user.Id);

        return new RegisterResponse
        {
            Message = "Account created. Check your email for a confirmation link before signing in.",
            CanSignIn = false
        };
    }

    // ========================================
    // Google sign-in (customers only)
    // ========================================

    public AuthenticationProperties BuildGoogleChallenge(string redirectUrl)
    {
        return _signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme, redirectUrl);
    }

    public string BuildUnavailableRedirect(string message)
    {
        return BuildFrontendRedirect("/login", message);
    }

    public async Task<string> CompleteGoogleSignInAsync(string? returnUrl)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            return BuildFrontendRedirect("/login", "Google sign-in was cancelled or timed out.");
        }

        var email = NormalizeEmail(info.Principal.FindFirstValue(ClaimTypes.Email));

        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (user == null && email.Length > 0)
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        if (user != null && await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole))
        {
            return BuildFrontendRedirect(
                "/login", "Admin accounts sign in with their email and password.");
        }

        if (user == null)
        {
            if (email.Length == 0)
            {
                return BuildFrontendRedirect(
                    "/login", "Google did not share an email address with us.");
            }

            user = await CreateGoogleCustomerAsync(info, email);
        }

        // Link this Google account to the user if it is not linked yet
        var logins = await _userManager.GetLoginsAsync(user);

        if (!logins.Any(login =>
                login.LoginProvider == info.LoginProvider &&
                login.ProviderKey == info.ProviderKey))
        {
            ThrowIfFailed(await _userManager.AddLoginAsync(user, info));
        }

        // Google has already verified the address
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await LinkCustomerProfileAsync(user);

        await _signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme);

        await SignInAsync(user, rememberMe: true);

        _logger.LogInformation("Customer account {UserId} signed in with Google", user.Id);

        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/dashboard" : returnUrl;

        return BuildFrontendRedirect(target, null);
    }

    private async Task<AppUser> CreateGoogleCustomerAsync(ExternalLoginInfo info, string email)
    {
        var name = info.Principal.FindFirstValue(ClaimTypes.Name)?.Trim();

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = string.IsNullOrEmpty(name) ? email : name,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // No password: this account signs in through Google
        ThrowIfFailed(await _userManager.CreateAsync(user));
        ThrowIfFailed(await _userManager.AddToRoleAsync(user, AuthConstants.CustomerRole));

        _logger.LogInformation("Customer account {UserId} created from Google", user.Id);

        return user;
    }

    private string BuildFrontendRedirect(string path, string? error)
    {
        var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
        var target = path.StartsWith('/') ? path : $"/{path}";

        return error == null
            ? $"{baseUrl}{target}"
            : $"{baseUrl}{target}?error={Uri.EscapeDataString(error)}";
    }

    // Every customer account points at a row in Customers, reusing one with the same email
    private async Task LinkCustomerProfileAsync(AppUser user)
    {
        if (!await _userManager.IsInRoleAsync(user, AuthConstants.CustomerRole))
        {
            return;
        }

        if (await _customerRepository.GetByAppUserIdAsync(user.Id) != null)
        {
            return;
        }

        var email = user.Email ?? string.Empty;
        var existing = await _customerRepository.GetByEmailAsync(email);

        if (existing != null)
        {
            existing.AppUserId = user.Id;
        }
        else
        {
            await _customerRepository.AddAsync(new Customer
            {
                Name = user.FullName,
                Email = email,
                AppUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    // ========================================
    // Sign in
    // ========================================

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (email.Length == 0 || string.IsNullOrEmpty(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            _userManager.PasswordHasher.VerifyHashedPassword(
                new AppUser(), DummyPasswordHash, request.Password);

            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new AuthenticationFailedException(LockedOutMessage);
        }

        if (!result.Succeeded)
        {
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new AuthenticationFailedException(
                "Please confirm your email address before signing in.");
        }

        await EnsureCorrectPortalAsync(user, request.Portal);

        var needsTwoFactor =
            await _userManager.GetTwoFactorEnabledAsync(user) &&
            !await _signInManager.IsTwoFactorClientRememberedAsync(user);

        if (needsTwoFactor)
        {
            // Let Identity store the short-lived two-factor cookie that the
            // verify step reads. This re-checks the password, which is cheap enough here.
            await _signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: false);

            return new LoginResponse { RequiresTwoFactor = true };
        }

        await SignInAsync(user, request.RememberMe);

        return new LoginResponse { User = await MapToResponseAsync(user) };
    }

    // Staff sign in on their own page, customers on theirs. Checked only after the
    // password is correct, so nobody can use it to find out which emails are admins.
    private async Task EnsureCorrectPortalAsync(AppUser user, string? portal)
    {
        var isAdmin = await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole);
        var wantsAdminPortal = string.Equals(
            portal, AuthConstants.AdminRole, StringComparison.OrdinalIgnoreCase);

        if (isAdmin && !wantsAdminPortal)
        {
            throw new ForbiddenException(
                "This is a staff account. Please use the staff sign-in page.");
        }

        if (!isAdmin && wantsAdminPortal)
        {
            throw new ForbiddenException(
                "This is a customer account. Please use the customer sign-in page.");
        }
    }

    public async Task<AuthUserResponse> VerifyTwoFactorAsync(TwoFactorLoginRequest request)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new AuthenticationFailedException(
                "Your sign-in session expired. Please sign in again.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AuthenticationFailedException(LockedOutMessage);
        }

        var isValid = request.IsRecoveryCode
            ? (await _userManager.RedeemTwoFactorRecoveryCodeAsync(
                user, NormalizeRecoveryCode(request.Code))).Succeeded
            : await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                NormalizeAuthenticatorCode(request.Code));

        if (!isValid)
        {
            await _userManager.AccessFailedAsync(user);

            throw new AuthenticationFailedException(
                request.IsRecoveryCode
                    ? "That recovery code is not valid."
                    : "That code is not valid. Check your authenticator app and try again.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (request.RememberMachine)
        {
            await _signInManager.RememberTwoFactorClientAsync(user);
        }

        await _signInManager.Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

        await SignInAsync(user, request.RememberMe);

        return await MapToResponseAsync(user);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);

        return user == null ? null : await MapToResponseAsync(user);
    }

    // ========================================
    // Email confirmation
    // ========================================

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            throw new ArgumentException(InvalidLinkMessage);
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        ThrowIfFailed(await _userManager.ConfirmEmailAsync(user, DecodeToken(request.Token)));

        _logger.LogInformation("Admin account {UserId} confirmed its email", user.Id);
    }

    // Always succeeds, so nobody can use it to discover which emails have accounts
    public async Task ResendConfirmationAsync(EmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(NormalizeEmail(request.Email));

        if (user != null && !user.EmailConfirmed)
        {
            await SendConfirmationEmailAsync(user);
        }
    }

    // ========================================
    // Passwords
    // ========================================

    // Always succeeds, for the same reason as above
    public async Task ForgotPasswordAsync(EmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(NormalizeEmail(request.Email));

        if (user == null)
        {
            return;
        }

        var token = EncodeToken(await _userManager.GeneratePasswordResetTokenAsync(user));

        var link = _frontendOptions.BuildLink(
            "reset-password",
            new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = token
            });

        await SendAsync(user, AuthEmailTemplates.ResetPassword(user.FullName, link));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            throw new ArgumentException(InvalidLinkMessage);
        }

        ThrowIfFailed(await _userManager.ResetPasswordAsync(
            user, DecodeToken(request.Token), request.Password ?? string.Empty));

        // Receiving the email proves the address works, and a reset clears any lockout
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Admin account {UserId} reset its password", user.Id);

        await SendAsync(user, AuthEmailTemplates.PasswordChanged(user.FullName));
    }

    public async Task ChangePasswordAsync(
        ClaimsPrincipal principal, ChangePasswordRequest request)
    {
        var user = await GetUserOrThrowAsync(principal);

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword ?? string.Empty,
            request.NewPassword ?? string.Empty);

        if (!result.Succeeded &&
            result.Errors.Any(error => error.Code == "PasswordMismatch"))
        {
            throw new AuthenticationFailedException("Your current password is not correct.");
        }

        ThrowIfFailed(result);

        // Changing the password rotates the security stamp, which signs out other sessions
        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("Admin account {UserId} changed its password", user.Id);

        await SendAsync(user, AuthEmailTemplates.PasswordChanged(user.FullName));
    }

    // ========================================
    // Two-factor authentication
    // ========================================

    public async Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserOrThrowAsync(principal);

        if (user.TwoFactorEnabled)
        {
            throw new ConflictException("Two-factor authentication is already enabled.");
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        key ??= string.Empty;

        return new TwoFactorSetupResponse
        {
            SharedKey = FormatKey(key),
            AuthenticatorUri = BuildAuthenticatorUri(user.Email ?? string.Empty, key)
        };
    }

    public async Task<RecoveryCodesResponse> EnableTwoFactorAsync(
        ClaimsPrincipal principal, TwoFactorCodeRequest request)
    {
        var user = await GetUserOrThrowAsync(principal);

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeAuthenticatorCode(request.Code));

        if (!isValid)
        {
            throw new ArgumentException(
                "That code is not valid. Check your authenticator app and try again.");
        }

        ThrowIfFailed(await _userManager.SetTwoFactorEnabledAsync(user, true));

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
            user, RecoveryCodeCount);

        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("Admin account {UserId} enabled two-factor authentication", user.Id);

        return new RecoveryCodesResponse { RecoveryCodes = codes?.ToList() ?? new List<string>() };
    }

    public async Task DisableTwoFactorAsync(ClaimsPrincipal principal, PasswordRequest request)
    {
        var user = await GetUserOrThrowAsync(principal);

        await VerifyPasswordAsync(user, request.Password);

        if (!user.TwoFactorEnabled)
        {
            return;
        }

        ThrowIfFailed(await _userManager.SetTwoFactorEnabledAsync(user, false));

        // Drop the old secret so re-enabling starts from a fresh QR code
        await _userManager.ResetAuthenticatorKeyAsync(user);

        await _signInManager.ForgetTwoFactorClientAsync();
        await _signInManager.RefreshSignInAsync(user);

        _logger.LogWarning(
            "Admin account {UserId} disabled two-factor authentication", user.Id);
    }

    public async Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal, PasswordRequest request)
    {
        var user = await GetUserOrThrowAsync(principal);

        await VerifyPasswordAsync(user, request.Password);

        if (!user.TwoFactorEnabled)
        {
            throw new ConflictException("Two-factor authentication is not enabled.");
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
            user, RecoveryCodeCount);

        return new RecoveryCodesResponse { RecoveryCodes = codes?.ToList() ?? new List<string>() };
    }

    // ========================================
    // Helpers
    // ========================================

    private async Task SignInAsync(AppUser user, bool rememberMe)
    {
        var now = DateTimeOffset.UtcNow;

        var properties = new AuthenticationProperties
        {
            // Persistent cookie survives closing the browser
            IsPersistent = rememberMe,
            IssuedUtc = now,
            ExpiresUtc = now.Add(rememberMe
                ? AuthConstants.RememberMeDuration
                : AuthConstants.SessionDuration),

            // Remember me lasts exactly 7 days; normal sessions slide while active
            AllowRefresh = !rememberMe
        };

        await _signInManager.SignInWithClaimsAsync(user, properties, []);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private async Task<AppUser> GetUserOrThrowAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);

        if (user == null)
        {
            throw new AuthenticationFailedException("Your session is no longer valid.");
        }

        return user;
    }

    private async Task VerifyPasswordAsync(AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password) ||
            !await _userManager.CheckPasswordAsync(user, password))
        {
            throw new AuthenticationFailedException("Your password is not correct.");
        }
    }

    private async Task SendConfirmationEmailAsync(AppUser user)
    {
        var token = EncodeToken(await _userManager.GenerateEmailConfirmationTokenAsync(user));

        var link = _frontendOptions.BuildLink(
            "confirm-email",
            new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = token
            });

        await SendAsync(user, AuthEmailTemplates.ConfirmEmail(user.FullName, link));
    }

    private async Task SendAsync(AppUser user, EmailContent content)
    {
        await _emailService.SendAsync(
            user.Email ?? string.Empty,
            user.FullName,
            content.Subject,
            content.HtmlBody,
            content.TextBody);
    }

    private bool IsRegistrationKeyValid(string? providedKey)
    {
        // Registration stays closed until a key is configured
        if (string.IsNullOrEmpty(_authOptions.RegistrationKey) ||
            string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(_authOptions.RegistrationKey));
    }

    // Identity tokens contain characters that do not survive a URL, so they travel base64url encoded
    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private static string DecodeToken(string? token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token ?? string.Empty));
        }
        catch (FormatException)
        {
            throw new ArgumentException(InvalidLinkMessage);
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(error =>
                error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            throw new ConflictException(DuplicateEmailMessage);
        }

        if (result.Errors.Any(error => error.Code == "InvalidToken"))
        {
            throw new ArgumentException(InvalidLinkMessage);
        }

        throw new ArgumentException(
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    // Authenticator apps show the 6 digits in groups
    private static string NormalizeAuthenticatorCode(string? code)
    {
        return (code ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Trim();
    }

    // Recovery codes are stored with their dash, so only the typing is tidied up
    private static string NormalizeRecoveryCode(string? code)
    {
        var value = (code ?? string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToUpperInvariant();

        // Accept a code typed without its dash
        if (!value.Contains('-') && value.Length == 10)
        {
            value = $"{value[..5]}-{value[5..]}";
        }

        return value;
    }

    private static string ValidateFullName(string? fullName)
    {
        var value = fullName?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            throw new ArgumentException("Full name is required.");
        }

        if (value.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Full name cannot be longer than {NameMaxLength} characters.");
        }

        return value;
    }

    private static string ValidateEmail(string? email)
    {
        var value = NormalizeEmail(email);

        if (!IsValidEmail(value))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        return value;
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsValidEmail(string email)
    {
        return email.Length <= EmailMaxLength &&
            MailAddress.TryCreate(email, out var address) &&
            address.Address == email;
    }

    // Shown in groups of four so it can be typed into an authenticator app by hand
    private static string FormatKey(string key)
    {
        var groups = Enumerable
            .Range(0, (key.Length + 3) / 4)
            .Select(index => key.Substring(index * 4, Math.Min(4, key.Length - (index * 4))));

        return string.Join(' ', groups).ToLowerInvariant();
    }

    private static string BuildAuthenticatorUri(string email, string key)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6&period=30",
            Uri.EscapeDataString(Issuer),
            Uri.EscapeDataString(email),
            key);
    }

    private async Task<AuthUserResponse> MapToResponseAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.Contains(AuthConstants.AdminRole)
                ? AuthConstants.AdminRole
                : AuthConstants.CustomerRole,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            HasPassword = await _userManager.HasPasswordAsync(user)
        };
    }
}
