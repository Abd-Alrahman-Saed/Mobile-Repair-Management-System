using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ICashSessionService
{
    /// <summary>The currently open shift with its running expected total, or null if the drawer is closed.</summary>
    Task<CashSessionDto?> GetCurrentAsync();

    /// <summary>Opens a shift with a counted starting balance. Fails if one is already open.
    /// Requires <c>Permission.ViewCashRegister</c>.</summary>
    Task<CashSessionDto> OpenAsync(decimal openingBalance);

    /// <summary>Closes the open shift, freezing its expected/counted/difference. Requires
    /// <c>Permission.ViewCashRegister</c>.</summary>
    Task<CashSessionDto> CloseAsync(decimal countedAmount, string notes);

    /// <summary>Closed shifts, newest first. Non-owners (no <c>ViewReports</c>) see only their own.</summary>
    Task<IEnumerable<CashSessionDto>> GetHistoryAsync();

    /// <summary>The id of the open session, or null. Used by cash-transaction services to tag new rows.</summary>
    Task<int?> GetCurrentSessionIdAsync();

    /// <summary>Throws if a closed shift exists for <paramref name="businessDate"/> and the current user
    /// is not an owner - so a receptionist can't edit a day whose drawer is already reconciled.</summary>
    Task EnsureNotLockedAsync(DateTime businessDate);
}
