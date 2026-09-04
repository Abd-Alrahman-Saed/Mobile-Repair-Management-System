namespace MRMS.Application.DTOs;

/// <param name="Kind">OutOfStock | LowStock | StaleTicket</param>
/// <param name="Severity">danger | warning</param>
public record AlertDto(string Kind, string Title, string Subtitle, string Severity);
