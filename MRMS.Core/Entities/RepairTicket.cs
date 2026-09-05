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

    public int? AssignedTechnicianId { get; set; }

    /// <summary>When the customer was last notified (WhatsApp) that the ticket is ready. Null = not notified yet.</summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>Total amount actually collected from the customer so far (deposits + partial payments).
    /// <c>Cost - PaidAmount</c> is what's still owed; can be negative if a deposit exceeds the (not yet
    /// finalized) cost.</summary>
    public decimal PaidAmount { get; set; }

    public Device Device { get; set; } = null!;
    public User? AssignedTechnician { get; set; }
    public ICollection<DevicePhoto> Photos { get; set; } = new List<DevicePhoto>();
    public Warranty? Warranty { get; set; }
    public ICollection<RepairTicketItem> RepairTicketItems { get; set; } = new List<RepairTicketItem>();
}
