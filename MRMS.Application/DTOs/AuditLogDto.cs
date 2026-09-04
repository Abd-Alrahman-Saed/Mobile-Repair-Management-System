namespace MRMS.Application.DTOs;

public record AuditLogDto(
    int Id,
    string UserName,
    string Action,
    string EntityType,
    int? EntityId,
    string Details,
    DateTime CreatedAt);
