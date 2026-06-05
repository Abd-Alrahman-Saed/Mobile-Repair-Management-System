namespace MRMS.Application.DTOs;

public record WarrantyDto(
    int Id,
    int RepairTicketId,
    int DurationDays,
    DateTime ExpiryDate,
    bool IsExpired);

public record AssignWarrantyDto(
    int RepairTicketId,
    int DurationDays,
    DateTime ExpiryDate);
