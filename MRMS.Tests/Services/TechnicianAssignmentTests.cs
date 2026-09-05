using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

public sealed class TechnicianAssignmentTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _ticketId;
    private int _technicianId;
    private int _receptionistId;

    public TechnicianAssignmentTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var customer = new Customer { FullName = "ع", PhoneNumber = "0" };
        var device = new Device { Brand = "S", Model = "A", Customer = customer };
        var ticket = new RepairTicket
        {
            TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Received,
            ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero
        };
        ctx.RepairTickets.Add(ticket);

        var tech = new User { Username = "tech1", FullName = "علي", Role = UserRole.Technician, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
        var inactiveTech = new User { Username = "tech2", FullName = "سعيد", Role = UserRole.Technician, IsActive = false, PasswordHash = "x", PasswordSalt = "y" };
        var reception = new User { Username = "r1", FullName = "منى", Role = UserRole.Receptionist, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
        ctx.Users.AddRange(tech, inactiveTech, reception);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
        _technicianId = tech.Id;
        _receptionistId = reception.Id;
    }

    public void Dispose() => _connection.Dispose();

    private RepairTicketService NewTicketService(UserRole role = UserRole.Owner)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(role);
        var perms = new PermissionService(current);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms), perms);
    }

    private UserService NewUserService()
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = new PermissionService(current);
        return new UserService(uow, perms, current, new AuditService(uow, current, perms));
    }

    [Fact]
    public async Task Assigning_an_active_technician_succeeds_and_is_audited()
    {
        await NewTicketService().AssignTechnicianAsync(_ticketId, _technicianId);

        await using var ctx = new AppDbContext(_options);
        (await ctx.RepairTickets.FindAsync(_ticketId))!.AssignedTechnicianId.Should().Be(_technicianId);
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "RepairTicket.TechnicianAssigned")).Should().BeTrue();
    }

    [Fact]
    public async Task Assigning_null_clears_the_assignment()
    {
        var service = NewTicketService();
        await service.AssignTechnicianAsync(_ticketId, _technicianId);

        await service.AssignTechnicianAsync(_ticketId, null);

        await using var ctx = new AppDbContext(_options);
        (await ctx.RepairTickets.FindAsync(_ticketId))!.AssignedTechnicianId.Should().BeNull();
    }

    [Fact]
    public async Task Assigning_an_inactive_or_non_technician_user_is_rejected()
    {
        var service = NewTicketService();

        await FluentActions.Awaiting(() => service.AssignTechnicianAsync(_ticketId, _receptionistId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetTechniciansAsync_returns_only_active_technicians_without_a_permission_check()
    {
        var techs = await NewUserService().GetTechniciansAsync();

        techs.Should().ContainSingle(t => t.Id == _technicianId);
        techs.Should().NotContain(t => t.Username == "tech2", "inactive");
        techs.Should().NotContain(t => t.Username == "r1", "not a technician");
    }
}
