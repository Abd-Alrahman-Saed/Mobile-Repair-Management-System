namespace MRMS.Application.DTOs;

public record LoginDto(string Password);

public record LoginResult(bool Success, string? ErrorMessage);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
