using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record RepairTicketDto(
    int Id,
    string TicketNumber,
    int DeviceId,
    string? DeviceInfo,
    string? CustomerName,
    string ProblemDescription,
    string ReceptionNotes,
    TicketStatus Status,
    DateTime ReceiveDate,
    TimeSpan ReceiveTime,
    string RepairDescription,
    decimal Cost,
    DateTime? DeliveryDate,
    TimeSpan? DeliveryTime,
    DateTime CreatedAt,
    List<RepairTicketPartDto>? Parts = null,
    decimal PartsCost = 0,
    int? AssignedTechnicianId = null,
    string? AssignedTechnicianName = null,
    string? CustomerPhone = null,
    DateTime? NotifiedAt = null,
    decimal PaidAmount = 0)
{
    /// <summary>What's still owed by the customer. Can be negative if a deposit exceeds the
    /// (not yet finalized) cost - that's expected, not an error.</summary>
    public decimal RemainingAmount => Cost - PaidAmount;
}

public record RepairTicketPartDto(
    int Id,
    int ItemId,
    string ItemName,
    int Quantity,
    decimal UnitCost,
    decimal TotalCost);

public record AddPartToTicketDto(
    int TicketId,
    int ItemId,
    int Quantity,
    decimal UnitCost);

public record CreateRepairTicketDto(
    int DeviceId,
    string ProblemDescription,
    string ReceptionNotes);

public record UpdateRepairTicketStatusDto(int TicketId, TicketStatus NewStatus, int? ApprovedByUserId = null);

public record UpdateRepairInfoDto(
    int TicketId,
    string RepairDescription,
    decimal Cost);

public record CollectPaymentDto(int TicketId, decimal Amount);
