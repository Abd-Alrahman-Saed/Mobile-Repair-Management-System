using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissions;

    public AuditService(IUnitOfWork uow, ICurrentUserService currentUser, IPermissionService permissions)
    {
        _uow = uow;
        _currentUser = currentUser;
        _permissions = permissions;
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

    public async Task<IReadOnlyList<AuditLogDto>> QueryAsync(
        DateTime? from = null, DateTime? to = null, string? userName = null, string? action = null)
    {
        _permissions.Require(Permission.ViewAuditLog);

        var query = _uow.AuditLogs.Query();

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value.Date);
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedAt <= end);
        }
        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(a => a.UserName.Contains(userName));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        var rows = await query
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Take(500)
            .ToListAsync();

        return rows.Select(a => new AuditLogDto(
            a.Id, a.UserName, a.Action, a.EntityType, a.EntityId, a.Details, a.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetActionsAsync()
    {
        _permissions.Require(Permission.ViewAuditLog);
        return await _uow.AuditLogs.Query()
            .Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
    }
}
