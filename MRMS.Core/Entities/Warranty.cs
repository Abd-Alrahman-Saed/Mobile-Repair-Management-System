namespace MRMS.Core.Entities;

public class Warranty
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public int DurationDays { get; set; }
    public DateTime ExpiryDate { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow > ExpiryDate;
}
