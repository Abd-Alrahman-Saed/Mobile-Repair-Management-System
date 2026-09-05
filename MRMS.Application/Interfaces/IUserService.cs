using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IUserService
{
    /// <summary>Requires <c>Permission.ManageUsers</c> (Owner only).</summary>
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    /// <summary>Requires <c>Permission.ManageUsers</c> (Owner only).</summary>
    Task<UserDto?> GetByIdAsync(int id);
    /// <summary>Requires <c>Permission.ManageUsers</c> (Owner only).</summary>
    Task<UserDto> CreateAsync(CreateUserDto dto);
    /// <summary>Requires <c>Permission.ManageUsers</c> (Owner only).</summary>
    Task<UserDto> UpdateAsync(UpdateUserDto dto);
    /// <summary>Requires <c>Permission.ManageUsers</c> (Owner only).</summary>
    Task ResetPasswordAsync(ResetPasswordDto dto);

    /// <summary>Active Technician-role users, for assignment pickers. Not permission-gated - front
    /// desk and technicians both need this to assign/see who's on a ticket.</summary>
    Task<IReadOnlyList<UserDto>> GetTechniciansAsync();
}
