using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

public class RepairTicket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string ReceptionNotes { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Received;
    public DateTime ReceiveDate { get; set; }
    public TimeSpan ReceiveTime { get; set; }
    public string RepairDescription { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public TimeSpan? DeliveryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Device Device { get; set; } = null!;
    public ICollection<DevicePhoto> Photos { get; set; } = new List<DevicePhoto>();
    public Warranty? Warranty { get; set; }
}
