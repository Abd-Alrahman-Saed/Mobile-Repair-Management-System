using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IAlertService
{
    /// <summary>Operational warnings for the notification bell: out-of-stock, low-stock, and tickets
    /// stuck waiting for parts. Not permission-gated - every role needs to see these.</summary>
    Task<IReadOnlyList<AlertDto>> GetAlertsAsync();
}
