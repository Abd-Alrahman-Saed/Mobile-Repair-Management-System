using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Receptionist;
    public bool IsActive { get; set; } = true;

    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>Base64 PBKDF2 salt. Null means <see cref="PasswordHash"/> is a legacy unsalted SHA-256
    /// hex hash and should be re-hashed on the next successful login.</summary>
    public string? PasswordSalt { get; set; }
    public bool MustChangePassword { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
