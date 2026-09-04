using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record UserDto(
    int Id,
    string Username,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime? LastLoginAt);

public record CreateUserDto(
    string Username,
    string FullName,
    UserRole Role,
    string Password);

public record UpdateUserDto(
    int Id,
    string FullName,
    UserRole Role,
    bool IsActive);

public record ResetPasswordDto(int UserId, string NewPassword);
