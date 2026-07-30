using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class RepairTicketService : IRepairTicketService
{
    private readonly IUnitOfWork _uow;
    private readonly IJournalService _journalService;

    public RepairTicketService(IUnitOfWork uow, IJournalService journalService)
    {
        _uow = uow;
        _journalService = journalService;
    }

    public async Task<RepairTicketDto> CreateAsync(CreateRepairTicketDto dto)
    {
        var ticket = new RepairTicket
        {
            TicketNumber = await GenerateTicketNumberAsync(),
            DeviceId = dto.DeviceId,
            ProblemDescription = dto.ProblemDescription,
            ReceptionNotes = dto.ReceptionNotes,
            Status = TicketStatus.Received,
            ReceiveDate = DateTime.UtcNow.Date,
            ReceiveTime = DateTime.UtcNow.TimeOfDay,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.RepairTickets.AddAsync(ticket);
        await _uow.CompleteAsync();
        return MapToDto(ticket);
    }

    public async Task<RepairTicketDto?> GetByIdAsync(int id)
    {
        var tickets = await _uow.RepairTickets.FindAsync(t => t.Id == id, t => t.Device, t => t.Device.Customer);
        var ticket = tickets.FirstOrDefault();
        if (ticket is null) return null;
        var dto = MapToDto(ticket);
        var parts = await GetPartsAsync(id);
        return dto with
        {
            DeviceInfo = $"{ticket.Device?.Brand} {ticket.Device?.Model}",
            CustomerName = ticket.Device?.Customer?.FullName,
            Parts = parts.ToList(),
            PartsCost = parts.Sum(p => p.TotalCost)
        };
    }

    public async Task<IEnumerable<RepairTicketDto>> GetByDeviceIdAsync(int deviceId)
    {
        var tickets = await _uow.RepairTickets.FindAsync(t => t.DeviceId == deviceId);
        return tickets.Select(MapToDto);
    }

    public async Task<bool> UpdateStatusAsync(UpdateRepairTicketStatusDto dto)
    {
        var ticket = await _uow.RepairTickets.GetByIdAsync(dto.TicketId);
        if (ticket is null) return false;

        if (ticket.Status == TicketStatus.Delivered || ticket.Status == TicketStatus.Cancelled)
            return false;

        var oldStatus = ticket.Status;
        ticket.Status = dto.NewStatus;

        if (dto.NewStatus == TicketStatus.Delivered)
        {
            ticket.DeliveryDate = DateTime.UtcNow.Date;
            ticket.DeliveryTime = DateTime.UtcNow.TimeOfDay;
        }

        if (dto.NewStatus == TicketStatus.InRepair && oldStatus != TicketStatus.InRepair)
            await DeductPartsStockAsync(dto.TicketId);

        _uow.RepairTickets.Update(ticket);
        await _uow.CompleteAsync();

        if (dto.NewStatus == TicketStatus.Delivered)
        {
            await _journalService.PostRepairDeliveryAsync(ticket.DeliveryDate!.Value, ticket.TicketNumber, ticket.Cost, ticket.Id);
            await _uow.CompleteAsync();
        }

        return true;
    }

    public async Task<bool> UpdateRepairInfoAsync(UpdateRepairInfoDto dto)
    {
        var ticket = await _uow.RepairTickets.GetByIdAsync(dto.TicketId);
        if (ticket is null) return false;

        ticket.RepairDescription = dto.RepairDescription;
        ticket.Cost = dto.Cost;
        _uow.RepairTickets.Update(ticket);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<IEnumerable<RepairTicketDto>> GetAllActiveAsync()
    {
        var tickets = await _uow.RepairTickets.FindAsync(
            t => t.Status != TicketStatus.Delivered
              && t.Status != TicketStatus.Cancelled,
            t => t.Device);
        return tickets.Select(t =>
        {
            var dto = MapToDto(t);
            return dto with
            {
                DeviceInfo = $"{t.Device?.Brand} {t.Device?.Model}"
            };
        });
    }

    public async Task<IEnumerable<RepairTicketDto>> GetAllAsync()
    {
        var tickets = await _uow.RepairTickets.FindAsync(_ => true, t => t.Device, t => t.Device.Customer);
        return tickets.Select(t =>
        {
            var dto = MapToDto(t);
            return dto with
            {
                DeviceInfo = $"{t.Device?.Brand} {t.Device?.Model}",
                CustomerName = t.Device?.Customer?.FullName
            };
        });
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var allTickets = await _uow.RepairTickets.GetAllAsync();
        var allCustomers = await _uow.Customers.GetAllAsync();
        var allDevices = await _uow.Devices.GetAllAsync();
        var today = DateTime.UtcNow.Date;

        return new DashboardStatsDto(
            TodayReceived: allTickets.Count(t => t.ReceiveDate == today),
            TodayDelivered: allTickets.Count(t => t.DeliveryDate == today),
            CurrentlyInRepair: allTickets.Count(t => t.Status is TicketStatus.UnderInspection
                                                         or TicketStatus.WaitingForParts
                                                         or TicketStatus.InRepair),
            WaitingForParts: allTickets.Count(t => t.Status == TicketStatus.WaitingForParts),
            TotalRepaired: allTickets.Count(t => t.Status == TicketStatus.Repaired),
            TotalDelivered: allTickets.Count(t => t.Status == TicketStatus.Delivered),
            TotalCustomers: allCustomers.Count(),
            TotalDevices: allDevices.Count(),
            TotalTickets: allTickets.Count()
        );
    }

    public async Task<IEnumerable<RepairTicketPartDto>> GetPartsAsync(int ticketId)
    {
        var items = await _uow.RepairTicketItems
            .Query()
            .Include(i => i.Item)
            .Where(i => i.RepairTicketId == ticketId)
            .ToListAsync();

        return items.Select(i => new RepairTicketPartDto(
            i.Id, i.ItemId, i.Item.Name, i.Quantity, i.UnitCost, i.TotalCost));
    }

    public async Task<RepairTicketPartDto> AddPartAsync(AddPartToTicketDto dto)
    {
        var part = new RepairTicketItem
        {
            RepairTicketId = dto.TicketId,
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost
        };

        var ticket = await _uow.RepairTickets.GetByIdAsync(dto.TicketId);
        if (ticket is not null && ticket.Status == TicketStatus.InRepair)
        {
            var item = await _uow.Items.GetByIdAsync(dto.ItemId);
            if (item is not null)
                item.CurrentStock -= dto.Quantity;
        }

        await _uow.RepairTicketItems.AddAsync(part);
        await _uow.CompleteAsync();

        var saved = await _uow.RepairTicketItems
            .Query()
            .Include(i => i.Item)
            .FirstAsync(i => i.Id == part.Id);

        return new RepairTicketPartDto(
            saved.Id, saved.ItemId, saved.Item.Name, saved.Quantity, saved.UnitCost, saved.TotalCost);
    }

    public async Task RemovePartAsync(int ticketItemId)
    {
        var part = await _uow.RepairTicketItems
            .Query()
            .Include(i => i.RepairTicket)
            .FirstOrDefaultAsync(i => i.Id == ticketItemId);

        if (part is null) return;

        if (part.RepairTicket.Status == TicketStatus.InRepair)
        {
            var item = await _uow.Items.GetByIdAsync(part.ItemId);
            if (item is not null)
                item.CurrentStock += part.Quantity;
        }

        _uow.RepairTicketItems.Delete(part);
        await _uow.CompleteAsync();
    }

    private async Task DeductPartsStockAsync(int ticketId)
    {
        var parts = await _uow.RepairTicketItems
            .Query()
            .Where(i => i.RepairTicketId == ticketId)
            .ToListAsync();

        foreach (var part in parts)
        {
            var item = await _uow.Items.GetByIdAsync(part.ItemId);
            if (item is not null)
                item.CurrentStock -= part.Quantity;
        }
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        var count = (await _uow.RepairTickets.GetAllAsync()).Count() + 1;
        return $"RPR-{count:D5}";
    }

    private static RepairTicketDto MapToDto(RepairTicket t) =>
        new(t.Id, t.TicketNumber, t.DeviceId, null, null,
            t.ProblemDescription, t.ReceptionNotes, t.Status,
            t.ReceiveDate, t.ReceiveTime, t.RepairDescription, t.Cost,
            t.DeliveryDate, t.DeliveryTime, t.CreatedAt);
}
