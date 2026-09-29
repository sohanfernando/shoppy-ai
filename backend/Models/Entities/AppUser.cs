namespace AdvancedOrderSystem.Models.Entities;

public class AdminUser
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // Always stored trimmed and lower-case
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
