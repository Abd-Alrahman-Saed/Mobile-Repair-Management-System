using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Queues an <c>AuditLog</c> row for the current user against the shared UnitOfWork. Does NOT save -
    /// the caller commits it inside its own transaction so the audit row and the business rows land together.
    /// </summary>
    Task LogAsync(string action, string entityType, int? entityId = null, string details = "");

    /// <summary>Read the audit log with optional filters. Requires <c>Permission.ViewAuditLog</c>.</summary>
    Task<IReadOnlyList<AuditLogDto>> QueryAsync(
        DateTime? from = null, DateTime? to = null, string? userName = null, string? action = null);

    /// <summary>Distinct <c>Action</c> values present in the log, for a filter dropdown.</summary>
    Task<IReadOnlyList<string>> GetActionsAsync();
}
