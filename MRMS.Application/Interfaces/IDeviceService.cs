using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IDeviceService
{
    Task<IEnumerable<DeviceDto>> GetByCustomerIdAsync(int customerId);
    Task<DeviceDto> RegisterAsync(RegisterDeviceDto dto);
    Task<bool> IsImeiDuplicatedAsync(string imei);
    Task<DeviceDto?> GetByImeiAsync(string imei);
    Task<bool> TransferOwnershipAsync(TransferDeviceDto dto);
    Task<DeviceDto?> GetByIdAsync(int id);
}
