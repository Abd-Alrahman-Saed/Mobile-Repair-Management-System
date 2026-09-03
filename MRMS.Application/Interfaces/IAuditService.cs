namespace MRMS.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Queues an <c>AuditLog</c> row for the current user against the shared UnitOfWork. Does NOT save -
    /// the caller commits it inside its own transaction so the audit row and the business rows land together.
    /// </summary>
    Task LogAsync(string action, string entityType, int? entityId = null, string details = "");
}
