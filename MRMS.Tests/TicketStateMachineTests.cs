using FluentAssertions;
using MRMS.Core;
using MRMS.Core.Enums;

namespace MRMS.Tests;

public class TicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.Received, TicketStatus.UnderInspection)]
    [InlineData(TicketStatus.Received, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.UnderInspection, TicketStatus.WaitingForParts)]
    [InlineData(TicketStatus.UnderInspection, TicketStatus.InRepair)]
    [InlineData(TicketStatus.UnderInspection, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.WaitingForParts, TicketStatus.InRepair)]
    [InlineData(TicketStatus.WaitingForParts, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.InRepair, TicketStatus.Repaired)]
    [InlineData(TicketStatus.InRepair, TicketStatus.WaitingForParts)]
    [InlineData(TicketStatus.InRepair, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.Repaired, TicketStatus.Delivered)]
    [InlineData(TicketStatus.Repaired, TicketStatus.InRepair)]
    public void Allowed_transitions_are_permitted(TicketStatus from, TicketStatus to) =>
        TicketStateMachine.CanTransition(from, to).Should().BeTrue();

    [Theory]
    [InlineData(TicketStatus.Received, TicketStatus.Delivered)]
    [InlineData(TicketStatus.Received, TicketStatus.InRepair)]
    [InlineData(TicketStatus.Received, TicketStatus.Repaired)]
    [InlineData(TicketStatus.UnderInspection, TicketStatus.Delivered)]
    [InlineData(TicketStatus.UnderInspection, TicketStatus.Repaired)]
    [InlineData(TicketStatus.WaitingForParts, TicketStatus.Repaired)]
    [InlineData(TicketStatus.WaitingForParts, TicketStatus.Delivered)]
    [InlineData(TicketStatus.InRepair, TicketStatus.Delivered)]
    [InlineData(TicketStatus.Repaired, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.Repaired, TicketStatus.UnderInspection)]
    [InlineData(TicketStatus.Delivered, TicketStatus.InRepair)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Received)]
    public void Skipping_or_reversing_steps_is_rejected(TicketStatus from, TicketStatus to) =>
        TicketStateMachine.CanTransition(from, to).Should().BeFalse();

    [Theory]
    [InlineData(TicketStatus.Delivered)]
    [InlineData(TicketStatus.Cancelled)]
    public void Terminal_states_allow_nothing(TicketStatus terminal) =>
        TicketStateMachine.AllowedNext(terminal).Should().BeEmpty();

    [Fact]
    public void No_status_can_transition_to_itself()
    {
        foreach (var status in Enum.GetValues<TicketStatus>())
            TicketStateMachine.CanTransition(status, status).Should().BeFalse($"{status} -> {status} is a no-op, not a transition");
    }
}
