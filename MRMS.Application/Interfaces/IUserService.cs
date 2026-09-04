using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

/// <summary>All methods require <c>Permission.ManageUsers</c> (Owner only).</summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(UpdateUserDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
