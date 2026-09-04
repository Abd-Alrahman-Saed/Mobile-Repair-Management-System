using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class AlertService : IAlertService
{
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingService _settings;

    public AlertService(IUnitOfWork uow, ISystemSettingService settings)
    {
        _uow = uow;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync()
    {
        var alerts = new List<AlertDto>();

        var items = await _uow.Items.GetAllAsync();
        foreach (var item in items.OrderBy(i => i.CurrentStock))
        {
            if (item.CurrentStock <= 0)
                alerts.Add(new AlertDto("OutOfStock", $"نفذ من المخزون: {item.Name}",
                    "الرصيد صفر", "danger"));
            else if (item.CurrentStock <= item.ReorderLevel)
                alerts.Add(new AlertDto("LowStock", $"مخزون منخفض: {item.Name}",
                    $"المتبقي {item.CurrentStock} (حد الطلب {item.ReorderLevel})", "warning"));
        }

        var staleDays = await StaleTicketDaysAsync();
        var cutoff = DateTime.UtcNow.Date.AddDays(-staleDays);
        var stale = await _uow.RepairTickets.Query()
            .Include(t => t.Device)
            .Where(t => t.Status == TicketStatus.WaitingForParts && t.ReceiveDate <= cutoff)
            .OrderBy(t => t.ReceiveDate)
            .ToListAsync();

        foreach (var t in stale)
        {
            var days = (DateTime.UtcNow.Date - t.ReceiveDate.Date).Days;
            alerts.Add(new AlertDto("StaleTicket", $"تذكرة راكدة: {t.TicketNumber}",
                $"بانتظار القطع منذ {days} يوم - {t.Device?.Brand} {t.Device?.Model}".Trim(), "warning"));
        }

        return alerts;
    }

    private async Task<int> StaleTicketDaysAsync()
    {
        var setting = await _settings.GetByKeyAsync("Alerts.StaleTicketDays");
        return int.TryParse(setting?.Value, out var n) && n > 0 ? n : 3;
    }
}
