using FluentAssertions;
using Moq;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidDto_CreatesCustomer()
    {
        var customers = new List<Customer>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Customers.AddAsync(It.IsAny<Customer>()))
            .Callback<Customer>(c => customers.Add(c));
        mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new CustomerService(mockUow.Object, MRMS.Tests.TestServices.Permissions());
        var result = await service.CreateAsync(new CreateCustomerDto("أحمد", "0123456789"));

        result.FullName.Should().Be("أحمد");
        result.PhoneNumber.Should().Be("0123456789");
    }

    [Fact]
    public async Task DeleteAsync_CustomerWithDevices_ReturnsTrue()
    {
        var mockUow = new Mock<IUnitOfWork>();
        var customer = new Customer { Id = 1, FullName = "test", PhoneNumber = "123" };

        mockUow.Setup(u => u.Customers.GetByIdAsync(1)).ReturnsAsync(customer);
        mockUow.Setup(u => u.Devices.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Device, bool>>>()))
            .ReturnsAsync(new List<Device>());

        var service = new CustomerService(mockUow.Object, MRMS.Tests.TestServices.Permissions());
        var result = await service.DeleteAsync(1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasDevicesOrRepairsAsync_WithDevices_ReturnsTrue()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Device, bool>>>()))
            .ReturnsAsync(new List<Device> { new() { Id = 1 } });

        var service = new CustomerService(mockUow.Object, MRMS.Tests.TestServices.Permissions());
        var result = await service.HasDevicesOrRepairsAsync(1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasDevicesOrRepairsAsync_NoDevices_ReturnsFalse()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Devices.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Device, bool>>>()))
            .ReturnsAsync(new List<Device>());

        var service = new CustomerService(mockUow.Object, MRMS.Tests.TestServices.Permissions());
        var result = await service.HasDevicesOrRepairsAsync(1);

        result.Should().BeFalse();
    }
}
