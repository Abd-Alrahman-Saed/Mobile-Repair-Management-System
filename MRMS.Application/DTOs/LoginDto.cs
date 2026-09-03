namespace MRMS.Application.DTOs;

public record LoginDto(string Username, string Password);

public record LoginResult(bool Success, string? ErrorMessage, bool MustChangePassword = false);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
