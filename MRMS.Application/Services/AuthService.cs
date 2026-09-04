using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Security;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AuthService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<LoginResult> LoginAsync(LoginDto login)
    {
        var username = login.Username.Trim();
        var users = await _uow.Users.FindAsync(u => u.Username == username);
        var user = users.FirstOrDefault();

        if (user is null || !user.IsActive)
            return new LoginResult(false, "اسم المستخدم أو كلمة المرور غير صحيحة");

        bool ok;
        if (user.PasswordSalt is null)
        {
            // Legacy unsalted SHA-256 hash - verify, and on success upgrade to PBKDF2 in place.
            ok = PasswordHasher.VerifyLegacySha256(login.Password, user.PasswordHash);
            if (ok)
            {
                (user.PasswordHash, user.PasswordSalt) = PasswordHasher.Hash(login.Password);
            }
        }
        else
        {
            ok = PasswordHasher.VerifyPbkdf2(login.Password, user.PasswordHash, user.PasswordSalt);
        }

        if (!ok)
            return new LoginResult(false, "اسم المستخدم أو كلمة المرور غير صحيحة");

        user.LastLoginAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        _currentUser.SetCurrentUser(new CurrentUser(user.Id, user.Username, user.FullName, user.Role));

        return new LoginResult(true, null, user.MustChangePassword);
    }

    public async Task<CurrentUser?> VerifyCredentialsAsync(string username, string password)
    {
        var name = username.Trim();
        var user = (await _uow.Users.FindAsync(u => u.Username == name)).FirstOrDefault();
        if (user is null || !user.IsActive)
            return null;

        var ok = user.PasswordSalt is null
            ? PasswordHasher.VerifyLegacySha256(password, user.PasswordHash)
            : PasswordHasher.VerifyPbkdf2(password, user.PasswordHash, user.PasswordSalt);

        return ok ? new CurrentUser(user.Id, user.Username, user.FullName, user.Role) : null;
    }

    public async Task<LoginResult> ChangePasswordAsync(ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
            return new LoginResult(false, "كلمة المرور الجديدة يجب أن تكون 4 أحرف على الأقل");

        var current = _currentUser.User;
        if (current is null)
            return new LoginResult(false, "لا يوجد مستخدم مسجّل دخوله");

        var user = await _uow.Users.GetByIdAsync(current.Id);
        if (user is null)
            return new LoginResult(false, "المستخدم غير موجود");

        var currentOk = user.PasswordSalt is null
            ? PasswordHasher.VerifyLegacySha256(dto.CurrentPassword, user.PasswordHash)
            : PasswordHasher.VerifyPbkdf2(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt);

        if (!currentOk)
            return new LoginResult(false, "كلمة المرور الحالية غير صحيحة");

        (user.PasswordHash, user.PasswordSalt) = PasswordHasher.Hash(dto.NewPassword);
        user.MustChangePassword = false;
        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        return new LoginResult(true, null);
    }
}
