namespace MRMS.Core.Entities;

public enum PhotoType { Before, After }

public class DevicePhoto
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public PhotoType PhotoType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RepairTicket RepairTicket { get; set; } = null!;
}
