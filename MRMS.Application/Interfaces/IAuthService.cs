using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginDto login);
    Task<LoginResult> ChangePasswordAsync(ChangePasswordDto dto);

    /// <summary>
    /// Checks a username/password without changing the signed-in user - for live "owner approval" of an
    /// over-limit action. Returns the matched active user, or null.
    /// </summary>
    Task<CurrentUser?> VerifyCredentialsAsync(string username, string password);
}
