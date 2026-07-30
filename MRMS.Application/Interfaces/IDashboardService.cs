using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime currentPeriodFrom, DateTime currentPeriodTo);
}
