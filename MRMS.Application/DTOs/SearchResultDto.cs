namespace MRMS.Application.DTOs;

public record SearchResultDto(
    int Id,
    string Type,
    string DisplayText,
    string? AdditionalInfo,
    int? CustomerId = null);
