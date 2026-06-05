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

        var users = await _uow.Users.FindAsync(
            u => u.Username == login.Username && u.PasswordHash == passwordHash);

        var user = users.FirstOrDefault();

        return user is null
            ? new LoginResult(false, "اسم المستخدم أو كلمة المرور غير صحيحة")
            : new LoginResult(true, null);
    }
}
