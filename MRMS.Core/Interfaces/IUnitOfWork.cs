using MRMS.Core.Entities;

namespace MRMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Device> Devices { get; }
    IRepository<RepairTicket> RepairTickets { get; }
    IRepository<DevicePhoto> DevicePhotos { get; }
    IRepository<Warranty> Warranties { get; }
    IRepository<Item> Items { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<SystemSetting> SystemSettings { get; }
    IRepository<RepairTicketItem> RepairTicketItems { get; }
    Task<int> CompleteAsync();
}
