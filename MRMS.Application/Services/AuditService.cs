using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string entityType, int? entityId = null, string details = "")
    {
        var user = _currentUser.User;
        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user?.Id,
            UserName = user?.FullName is { Length: > 0 } fn ? fn : user?.Username ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }
}
