namespace MRMS.Core.Entities;

/// <summary>
/// One row per sensitive action (sale, purchase, expense, supplier payment, stock movement, ticket
/// status change, later: voids and user changes). <see cref="UserName"/> is a snapshot taken at write
/// time so the log still reads correctly if the user is later renamed or deactivated.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
