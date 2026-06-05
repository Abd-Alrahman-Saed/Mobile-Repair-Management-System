namespace MRMS.Application.DTOs;

public record DeviceDto(
    int Id,
    string IMEI,
    string Brand,
    string Model,
    string Color,
    int CustomerId,
    string? CustomerName,
    DateTime CreatedAt);

public record RegisterDeviceDto(
    string IMEI,
    string Brand,
    string Model,
    string Color,
    int CustomerId);

public record TransferDeviceDto(int DeviceId, int NewCustomerId);
