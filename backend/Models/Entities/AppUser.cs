using Microsoft.AspNetCore.Identity;

namespace AdvancedOrderSystem.Models.Entities;

// Identity owns Email, UserName, PasswordHash, lockout, 2FA and the security stamp
public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
