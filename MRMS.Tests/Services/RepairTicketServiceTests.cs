using FluentAssertions;
using Moq;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Tests.Services;

public class RepairTicketServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidDto_CreatesTicketWithReceivedStatus()
    {
        var tickets = new List<RepairTicket>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.RepairTickets.AddAsync(It.IsAny<RepairTicket>()))
            .Callback<RepairTicket>(t => tickets.Add(t));
        mockUow.Setup(u => u.RepairTickets.GetAllAsync())
            .ReturnsAsync(tickets);
        mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new RepairTicketService(mockUow.Object);
        var result = await service.CreateAsync(
            new CreateRepairTicketDto(1, "الشاشة مكسورة", "يرجى الفحص"));

        result.Status.Should().Be(TicketStatus.Received);
        result.ProblemDescription.Should().Be("الشاشة مكسورة");
        result.TicketNumber.Should().StartWith("RPR-");
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDelivered_SetsDeliveryDateTime()
    {
        var ticket = new RepairTicket
        {
            Id = 1,
            Status = TicketStatus.Repaired,
            DeviceId = 1,
            TicketNumber = "RPR-00001",
            ReceiveDate = DateTime.UtcNow.Date,
            ReceiveTime = TimeSpan.Zero
        };

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.RepairTickets.GetByIdAsync(1)).ReturnsAsync(ticket);
        mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new RepairTicketService(mockUow.Object);
        var result = await service.UpdateStatusAsync(
            new UpdateRepairTicketStatusDto(1, TicketStatus.Delivered));

        result.Should().BeTrue();
        ticket.DeliveryDate.Should().Be(DateTime.UtcNow.Date);
        ticket.DeliveryTime.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_FromDelivered_ReturnsFalse()
    {
        var ticket = new RepairTicket
        {
            Id = 1,
            Status = TicketStatus.Delivered,
            DeviceId = 1,
            TicketNumber = "RPR-00001",
            DeliveryDate = DateTime.UtcNow.Date,
            DeliveryTime = TimeSpan.Zero,
            ReceiveDate = DateTime.UtcNow.Date,
            ReceiveTime = TimeSpan.Zero
        };

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.RepairTickets.GetByIdAsync(1)).ReturnsAsync(ticket);

        var service = new RepairTicketService(mockUow.Object);
        var result = await service.UpdateStatusAsync(
            new UpdateRepairTicketStatusDto(1, TicketStatus.InRepair));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsCorrectCounts()
    {
        var today = DateTime.UtcNow.Date;
        var tickets = new List<RepairTicket>
        {
            new() { Id = 1, Status = TicketStatus.Received, ReceiveDate = today, DeviceId = 1, TicketNumber = "RPR-00001", ReceiveTime = TimeSpan.Zero },
            new() { Id = 2, Status = TicketStatus.InRepair, ReceiveDate = today, DeviceId = 1, TicketNumber = "RPR-00002", ReceiveTime = TimeSpan.Zero },
            new() { Id = 3, Status = TicketStatus.Delivered, ReceiveDate = today.AddDays(-1), DeliveryDate = today, DeviceId = 1, TicketNumber = "RPR-00003", ReceiveTime = TimeSpan.Zero },
            new() { Id = 4, Status = TicketStatus.Delivered, ReceiveDate = today.AddDays(-2), DeliveryDate = today, DeviceId = 1, TicketNumber = "RPR-00004", ReceiveTime = TimeSpan.Zero }
        };

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.RepairTickets.GetAllAsync()).ReturnsAsync(tickets);
        mockUow.Setup(u => u.Customers.GetAllAsync()).ReturnsAsync(new List<Customer>());
        mockUow.Setup(u => u.Devices.GetAllAsync()).ReturnsAsync(new List<Device>());

        var service = new RepairTicketService(mockUow.Object);
        var stats = await service.GetDashboardStatsAsync();

        stats.TodayReceived.Should().Be(2);
        stats.TodayDelivered.Should().Be(2);
        stats.CurrentlyInRepair.Should().Be(2);
    }
}
