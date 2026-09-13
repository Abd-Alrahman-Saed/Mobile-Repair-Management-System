using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core;
using MRMS.Core.Authorization;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class RepairTicketService : IRepairTicketService
{
    private readonly IUnitOfWork _uow;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICashSessionService _cashSessions;
    private readonly ICurrentUserService _currentUser;

    public RepairTicketService(IUnitOfWork uow, IJournalService journalService, IAuditService audit,
        IPermissionService permissions, ICashSessionService cashSessions, ICurrentUserService currentUser)
    {
        _uow = uow;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _cashSessions = cashSessions;
        _currentUser = currentUser;
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
        var tickets = await _uow.RepairTickets.FindAsync(t => t.Id == id,
            t => t.Device, t => t.Device.Customer, t => t.AssignedTechnician!);
        var ticket = tickets.FirstOrDefault();
        if (ticket is null) return null;
        var dto = MapToDto(ticket);
        var parts = await GetPartsAsync(id);
        return dto with
        {
            DeviceInfo = $"{ticket.Device?.Brand} {ticket.Device?.Model}",
            CustomerName = ticket.Device?.Customer?.FullName,
            CustomerPhone = ticket.Device?.Customer?.PhoneNumber,
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

        var oldStatus = ticket.Status;
        if (!TicketStateMachine.CanTransition(oldStatus, dto.NewStatus))
            return false;

        if (dto.NewStatus == TicketStatus.Delivered && ticket.Cost - ticket.PaidAmount > 0)
        {
            var approver = await ResolveOwnerApproverAsync(dto.ApprovedByUserId);
            if (approver is null)
                throw new InvalidOperationException(
                    "لا يمكن تسليم التذكرة والمبلغ المتبقي أكبر من صفر - يتطلب موافقة المالك");
        }

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            ticket.Status = dto.NewStatus;

            if (dto.NewStatus == TicketStatus.Delivered)
            {
                ticket.DeliveryDate = DateTime.UtcNow.Date;
                ticket.DeliveryTime = DateTime.UtcNow.TimeOfDay;
            }

            _uow.RepairTickets.Update(ticket);
            await _audit.LogAsync("RepairTicket.StatusChange", nameof(RepairTicket), ticket.Id,
                $"{ticket.TicketNumber}: {oldStatus} → {dto.NewStatus}");
            await _uow.CompleteAsync();

            if (dto.NewStatus == TicketStatus.Delivered)
            {
                // Revenue was already recognized incrementally as payments came in via
                // CollectPaymentAsync - only the parts COGS entry belongs here.
                var parts = await GetPartsAsync(dto.TicketId);
                var partsCost = parts.Sum(p => p.TotalCost);
                await _journalService.PostRepairPartsCostAsync(ticket.DeliveryDate!.Value, ticket.TicketNumber, partsCost, ticket.Id);
                await _uow.CompleteAsync();
            }
        });

        return true;
    }

    /// <summary>Validates a live owner-approval id the same way <c>SaleService</c> does: the approver
    /// must be an active user whose role holds <c>ApplyDiscountBeyondLimit</c> (the owner-only
    /// permission reused as the general "override with owner approval" gate).</summary>
    private async Task<User?> ResolveOwnerApproverAsync(int? approverId)
    {
        if (approverId is not { } id) return null;
        var user = await _uow.Users.GetByIdAsync(id);
        if (user is null || !user.IsActive || !RolePermissions.Has(user.Role, Permission.ApplyDiscountBeyondLimit))
            return null;
        return user;
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
            t => t.Device, t => t.AssignedTechnician!);
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
        var tickets = await _uow.RepairTickets.FindAsync(_ => true,
            t => t.Device, t => t.Device.Customer, t => t.AssignedTechnician!);
        return tickets.Select(t =>
        {
            var dto = MapToDto(t);
            return dto with
            {
                DeviceInfo = $"{t.Device?.Brand} {t.Device?.Model}",
                CustomerName = t.Device?.Customer?.FullName,
                CustomerPhone = t.Device?.Customer?.PhoneNumber
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
        _permissions.Require(Permission.DeductParts);

        var item = await _uow.Items.GetByIdAsync(dto.ItemId)
            ?? throw new InvalidOperationException("الصنف غير موجود");
        if (item.CurrentStock < dto.Quantity)
            throw new InvalidOperationException($"الرصيد غير كافٍ. المتوفر: {item.CurrentStock}، المطلوب: {dto.Quantity}");

        var part = new RepairTicketItem
        {
            RepairTicketId = dto.TicketId,
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost
        };

        // Stock leaves the moment the part is attached to the ticket - regardless of the ticket's
        // current status - and only ever moves once. It used to be tied to the ticket entering
        // InRepair, which meant a part added before that point was never deducted, and a ticket that
        // cycled back into InRepair had every part deducted a second time.
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            item.CurrentStock -= dto.Quantity;
            await _uow.RepairTicketItems.AddAsync(part);
            await _audit.LogAsync("RepairTicket.PartAdded", nameof(RepairTicketItem), part.Id,
                $"{item.Name} x{dto.Quantity}");
            await _uow.CompleteAsync();
        });

        var saved = await _uow.RepairTicketItems
            .Query()
            .Include(i => i.Item)
            .FirstAsync(i => i.Id == part.Id);

        return new RepairTicketPartDto(
            saved.Id, saved.ItemId, saved.Item.Name, saved.Quantity, saved.UnitCost, saved.TotalCost);
    }

    public async Task RemovePartAsync(int ticketItemId)
    {
        _permissions.Require(Permission.DeductParts);

        var part = await _uow.RepairTicketItems
            .Query()
            .Include(i => i.Item)
            .FirstOrDefaultAsync(i => i.Id == ticketItemId);

        if (part is null) return;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var item = await _uow.Items.GetByIdAsync(part.ItemId);
            if (item is not null)
                item.CurrentStock += part.Quantity;

            _uow.RepairTicketItems.Delete(part);
            await _audit.LogAsync("RepairTicket.PartRemoved", nameof(RepairTicketItem), part.Id,
                $"{part.Item.Name} x{part.Quantity}");
            await _uow.CompleteAsync();
        });
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        var count = (await _uow.RepairTickets.GetAllAsync()).Count() + 1;
        return $"RPR-{count:D5}";
    }

    public async Task AssignTechnicianAsync(int ticketId, int? technicianId)
    {
        _permissions.Require(Permission.UpdateTicketStatus);

        var ticket = await _uow.RepairTickets.GetByIdAsync(ticketId)
            ?? throw new InvalidOperationException("التذكرة غير موجودة");

        string? technicianName = null;
        if (technicianId is { } id)
        {
            var technician = await _uow.Users.GetByIdAsync(id);
            if (technician is null || !technician.IsActive || technician.Role != UserRole.Technician)
                throw new InvalidOperationException("المستخدم المحدد ليس فنياً نشطاً");
            technicianName = technician.FullName;
        }

        ticket.AssignedTechnicianId = technicianId;
        _uow.RepairTickets.Update(ticket);
        await _audit.LogAsync("RepairTicket.TechnicianAssigned", nameof(RepairTicket), ticket.Id,
            technicianId is null ? "إلغاء التخصيص" : technicianName ?? "");
        await _uow.CompleteAsync();
    }

    public async Task MarkNotifiedAsync(int ticketId)
    {
        _permissions.Require(Permission.UpdateTicketStatus);

        var ticket = await _uow.RepairTickets.GetByIdAsync(ticketId)
            ?? throw new InvalidOperationException("التذكرة غير موجودة");

        ticket.NotifiedAt = DateTime.UtcNow;
        _uow.RepairTickets.Update(ticket);
        await _audit.LogAsync("RepairTicket.CustomerNotified", nameof(RepairTicket), ticket.Id, ticket.TicketNumber);
        await _uow.CompleteAsync();
    }

    public async Task<RepairTicketDto> CollectPaymentAsync(CollectPaymentDto dto)
    {
        _permissions.Require(Permission.CollectPayment);

        if (dto.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        var ticket = await _uow.RepairTickets.GetByIdAsync(dto.TicketId)
            ?? throw new InvalidOperationException("التذكرة غير موجودة");

        await _cashSessions.EnsureNotLockedAsync(DateTime.Today);
        var sessionId = await _cashSessions.GetCurrentSessionIdAsync();
        var user = _currentUser.User;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            ticket.PaidAmount += dto.Amount;
            _uow.RepairTickets.Update(ticket);

            await _uow.RepairTicketPayments.AddAsync(new RepairTicketPayment
            {
                RepairTicketId = ticket.Id,
                Amount = dto.Amount,
                BusinessDate = DateTime.Today,
                CollectedAt = DateTime.UtcNow,
                CollectedByUserId = user?.Id ?? 0,
                CollectedByName = user?.FullName ?? user?.Username ?? "",
                CashSessionId = sessionId
            });

            await _audit.LogAsync("RepairTicket.PaymentCollected", nameof(RepairTicket), ticket.Id,
                $"{ticket.TicketNumber}: {dto.Amount:N2}");
            await _uow.CompleteAsync();

            await _journalService.PostRepairPaymentAsync(DateTime.UtcNow.Date, ticket.TicketNumber, dto.Amount, ticket.Id);
            await _uow.CompleteAsync();
        });

        return (await GetByIdAsync(ticket.Id))!;
    }

    private static RepairTicketDto MapToDto(RepairTicket t) =>
        new(t.Id, t.TicketNumber, t.DeviceId, null, null,
            t.ProblemDescription, t.ReceptionNotes, t.Status,
            t.ReceiveDate, t.ReceiveTime, t.RepairDescription, t.Cost,
            t.DeliveryDate, t.DeliveryTime, t.CreatedAt,
            AssignedTechnicianId: t.AssignedTechnicianId,
            AssignedTechnicianName: t.AssignedTechnician?.FullName,
            NotifiedAt: t.NotifiedAt,
            PaidAmount: t.PaidAmount);
}
