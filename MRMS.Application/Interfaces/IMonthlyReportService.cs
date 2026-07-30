using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IMonthlyReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(int year, int month);
}
