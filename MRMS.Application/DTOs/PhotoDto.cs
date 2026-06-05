using MRMS.Core.Entities;

namespace MRMS.Application.DTOs;

public record PhotoDto(
    int Id,
    int RepairTicketId,
    string FilePath,
    PhotoType PhotoType,
    DateTime CreatedAt);
