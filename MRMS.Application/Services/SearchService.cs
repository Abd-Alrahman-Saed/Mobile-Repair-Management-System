using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SearchService : ISearchService
{
    private readonly IUnitOfWork _uow;

    public SearchService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<SearchResultDto>> SearchAsync(string query)
    {
        var results = new List<SearchResultDto>();

        // Search devices by IMEI
        var devices = await _uow.Devices.FindAsync(d => d.IMEI.Contains(query));

        foreach (var d in devices)
        {
            var customer = await _uow.Customers.GetByIdAsync(d.CustomerId);
            var customerName = customer?.FullName ?? $"(ID: {d.CustomerId})";
            results.Add(new SearchResultDto(
                d.Id, "جهاز", $"{d.Brand} {d.Model} - {d.IMEI}",
                $"العميل: {customerName}", d.CustomerId));
        }

        // Search repair tickets by TicketNumber
        var tickets = await _uow.RepairTickets.FindAsync(
            t => t.TicketNumber.Contains(query),
            t => t.Device);

        foreach (var t in tickets)
        {
            var deviceInfo = $"{t.Device.Brand} {t.Device.Model}";
            results.Add(new SearchResultDto(
                t.Id, "تذكرة", $"{t.TicketNumber} - {deviceInfo}",
                $"المشكلة: {t.ProblemDescription}", t.Device.CustomerId));
        }

        return results;
    }
}
