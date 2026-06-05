namespace MRMS.Core.Entities;

public class Device
{
    public int Id { get; set; }
    public string IMEI { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public ICollection<RepairTicket> RepairTickets { get; set; } = new List<RepairTicket>();
}
