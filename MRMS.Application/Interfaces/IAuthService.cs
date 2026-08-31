using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginDto login);
    Task<LoginResult> ChangePasswordAsync(ChangePasswordDto dto);
}
