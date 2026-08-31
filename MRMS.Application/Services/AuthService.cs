using System.Security.Cryptography;
using System.Text;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;

    public AuthService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<LoginResult> LoginAsync(LoginDto login)
    {
        var passwordHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.Password)));

        // Single shared password for the shop, not a per-user identity - match on the password hash alone.
        var users = await _uow.Users.FindAsync(u => u.PasswordHash == passwordHash);
        var user = users.FirstOrDefault();

        return user is null
            ? new LoginResult(false, "كلمة المرور غير صحيحة")
            : new LoginResult(true, null);
    }

    public async Task<LoginResult> ChangePasswordAsync(ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
            return new LoginResult(false, "كلمة المرور الجديدة يجب أن تكون 4 أحرف على الأقل");

        var currentHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(dto.CurrentPassword)));

        var users = await _uow.Users.FindAsync(u => u.PasswordHash == currentHash);
        var user = users.FirstOrDefault();

        if (user is null)
            return new LoginResult(false, "كلمة المرور الحالية غير صحيحة");

        user.PasswordHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(dto.NewPassword)));
        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        return new LoginResult(true, null);
    }
}
