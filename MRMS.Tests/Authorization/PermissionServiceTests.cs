using FluentAssertions;
using MRMS.Application.Services;
using MRMS.Core.Authorization;
using MRMS.Core.Enums;

namespace MRMS.Tests.Authorization;

public class PermissionServiceTests
{
    private static PermissionService For(UserRole role) =>
        new(FakeCurrentUserService.As(role));

    [Fact]
    public void Owner_can_do_everything()
    {
        var owner = For(UserRole.Owner);
        foreach (var p in Enum.GetValues<Permission>())
            owner.Can(p).Should().BeTrue($"owner should have {p}");
    }

    [Fact]
    public void Receptionist_cannot_see_profit_or_reports()
    {
        var reception = For(UserRole.Receptionist);
        reception.Can(Permission.ViewProfit).Should().BeFalse();
        reception.Can(Permission.ViewReports).Should().BeFalse();
        reception.Can(Permission.ManageSuppliers).Should().BeFalse();
    }

    [Fact]
    public void Receptionist_can_do_front_desk_work()
    {
        var reception = For(UserRole.Receptionist);
        reception.Can(Permission.ReceiveDevice).Should().BeTrue();
        reception.Can(Permission.CollectPayment).Should().BeTrue();
    }

    [Fact]
    public void Require_throws_for_a_denied_permission()
    {
        var reception = For(UserRole.Receptionist);
        var act = () => reception.Require(Permission.ViewReports);
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void No_signed_in_user_means_no_permissions()
    {
        var anon = new FakeCurrentUserService();
        anon.Clear();
        new PermissionService(anon).Can(Permission.ReceiveDevice).Should().BeFalse();
    }

    [Fact]
    public void Owner_only_permissions_are_not_granted_to_manager()
    {
        RolePermissions.Has(UserRole.Manager, Permission.ManageUsers).Should().BeFalse();
        RolePermissions.Has(UserRole.Manager, Permission.Backup).Should().BeFalse();
        RolePermissions.Has(UserRole.Manager, Permission.ViewAuditLog).Should().BeFalse();
    }
}
