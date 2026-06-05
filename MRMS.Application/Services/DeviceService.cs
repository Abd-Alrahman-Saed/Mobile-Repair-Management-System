using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly IUnitOfWork _uow;

    public DeviceService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<DeviceDto>> GetByCustomerIdAsync(int customerId)
    {
        var devices = await _uow.Devices.FindAsync(d => d.CustomerId == customerId);
        return devices.Select(MapToDto);
    }

    public async Task<DeviceDto> RegisterAsync(RegisterDeviceDto dto)
    {
        var device = new Device
        {
            IMEI = dto.IMEI,
            Brand = dto.Brand,
            Model = dto.Model,
            Color = dto.Color,
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Devices.AddAsync(device);
        await _uow.CompleteAsync();
        return MapToDto(device);
    }

    public async Task<bool> IsImeiDuplicatedAsync(string imei)
    {
        var existing = await _uow.Devices.FindAsync(d => d.IMEI == imei);
        return existing.Any();
    }

    public async Task<DeviceDto?> GetByImeiAsync(string imei)
    {
        var devices = await _uow.Devices.FindAsync(d => d.IMEI == imei);
        var device = devices.FirstOrDefault();
        return device is null ? null : MapToDto(device);
    }

    public async Task<bool> TransferOwnershipAsync(TransferDeviceDto dto)
    {
        var device = await _uow.Devices.GetByIdAsync(dto.DeviceId);
        if (device is null) return false;

        device.CustomerId = dto.NewCustomerId;
        _uow.Devices.Update(device);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<DeviceDto?> GetByIdAsync(int id)
    {
        var device = await _uow.Devices.GetByIdAsync(id);
        return device is null ? null : MapToDto(device);
    }

    private static DeviceDto MapToDto(Device d) =>
        new(d.Id, d.IMEI, d.Brand, d.Model, d.Color, d.CustomerId, null, d.CreatedAt);
}
