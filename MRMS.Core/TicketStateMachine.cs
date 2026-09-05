using MRMS.Core.Enums;

namespace MRMS.Core;

/// <summary>
/// The single source of which ticket status transitions are legal. Before this existed the only rule
/// enforced was "don't leave Delivered/Cancelled" - any other jump (e.g. Received straight to Delivered,
/// skipping inspection and repair entirely) was silently allowed.
/// </summary>
public static class TicketStateMachine
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> Map =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Received] = [TicketStatus.UnderInspection, TicketStatus.Cancelled],
            [TicketStatus.UnderInspection] = [TicketStatus.WaitingForParts, TicketStatus.InRepair, TicketStatus.Cancelled],
            [TicketStatus.WaitingForParts] = [TicketStatus.InRepair, TicketStatus.Cancelled],
            [TicketStatus.InRepair] = [TicketStatus.Repaired, TicketStatus.WaitingForParts, TicketStatus.Cancelled],
            [TicketStatus.Repaired] = [TicketStatus.Delivered, TicketStatus.InRepair],
            [TicketStatus.Delivered] = [],
            [TicketStatus.Cancelled] = [],
        };

    public static IReadOnlyList<TicketStatus> AllowedNext(TicketStatus current) =>
        Map.TryGetValue(current, out var next) ? next : [];

    public static bool CanTransition(TicketStatus current, TicketStatus next) =>
        AllowedNext(current).Contains(next);
}
