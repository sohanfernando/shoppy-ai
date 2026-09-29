using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdvancedOrderSystem.Auth;

// Password reset links expire after 1 hour instead of Identity's default 1 day
public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public PasswordResetTokenProviderOptions()
    {
        Name = AuthConstants.PasswordResetTokenProvider;
        TokenLifespan = AuthConstants.PasswordResetTokenLifespan;
    }
}

public class PasswordResetTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
    where TUser : class
{
    public PasswordResetTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PasswordResetTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<TUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
