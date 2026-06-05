namespace MRMS.Application.DTOs;

public record LoginDto(string Username, string Password);

public record LoginResult(bool Success, string? ErrorMessage);
