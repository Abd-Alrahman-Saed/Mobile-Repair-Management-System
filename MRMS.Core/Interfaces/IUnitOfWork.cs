using MRMS.Core.Entities;

namespace MRMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Entities.Customer> Customers { get; }
    IRepository<Entities.Device> Devices { get; }
    IRepository<Entities.RepairTicket> RepairTickets { get; }
    IRepository<Entities.DevicePhoto> DevicePhotos { get; }
    IRepository<Entities.Warranty> Warranties { get; }
    Task<int> CompleteAsync();
}
