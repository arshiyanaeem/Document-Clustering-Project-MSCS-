namespace DSFS.Core.Models;

public enum UserRole
{
    Admin,
    Scholar
}

/// <summary>
/// Represents an account in the DSFS User Interface phase (Registration Module / User Profile Module,
/// section 5.4.2 of the thesis). Passwords are never stored in plain text - see
/// <see cref="DSFS.Core.Security.PasswordHasher"/>.
/// </summary>
public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ResearchDomain { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Scholar;

    /// <summary>Base64-encoded PBKDF2 password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Base64-encoded random salt used to compute <see cref="PasswordHash"/>.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
