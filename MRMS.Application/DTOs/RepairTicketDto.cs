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
    DateTime CreatedAt);

public record CreateRepairTicketDto(
    int DeviceId,
    string ProblemDescription,
    string ReceptionNotes);

public record UpdateRepairTicketStatusDto(int TicketId, TicketStatus NewStatus);

public record UpdateRepairInfoDto(
    int TicketId,
    string RepairDescription,
    decimal Cost);
