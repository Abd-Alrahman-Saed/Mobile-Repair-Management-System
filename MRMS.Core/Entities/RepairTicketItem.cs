namespace MRMS.Core.Entities;

public class RepairTicketItem
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
