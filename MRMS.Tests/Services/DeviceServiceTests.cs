using FluentAssertions;
using Moq;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Tests.Services;

public class DeviceServiceTests
{
    [Fact]
    public async Task RegisterAsync_ValidDto_RegistersDevice()
    {
        var devices = new List<Device>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.AddAsync(It.IsAny<Device>()))
            .Callback<Device>(d => devices.Add(d));
        mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new DeviceService(mockUow.Object);
        var result = await service.RegisterAsync(
            new RegisterDeviceDto("123456789012345", "Samsung", "A52", "Black", 1));

        result.IMEI.Should().Be("123456789012345");
        result.Brand.Should().Be("Samsung");
    }

    [Fact]
    public async Task IsImeiDuplicatedAsync_DuplicateExists_ReturnsTrue()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Device, bool>>>()))
            .ReturnsAsync(new List<Device> { new() { IMEI = "123456789012345" } });

        var service = new DeviceService(mockUow.Object);
        var result = await service.IsImeiDuplicatedAsync("123456789012345");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsImeiDuplicatedAsync_NoDuplicate_ReturnsFalse()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Device, bool>>>()))
            .ReturnsAsync(new List<Device>());

        var service = new DeviceService(mockUow.Object);
        var result = await service.IsImeiDuplicatedAsync("123456789012345");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TransferOwnershipAsync_ValidTransfer_UpdatesCustomerId()
    {
        var device = new Device { Id = 1, CustomerId = 1 };
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.GetByIdAsync(1)).ReturnsAsync(device);
        mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new DeviceService(mockUow.Object);
        var result = await service.TransferOwnershipAsync(
            new TransferDeviceDto(1, 2));

        result.Should().BeTrue();
        device.CustomerId.Should().Be(2);
    }
}
