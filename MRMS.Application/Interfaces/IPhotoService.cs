using MRMS.Application.DTOs;
using MRMS.Core.Entities;

namespace MRMS.Application.Interfaces;

public interface IPhotoService
{
    Task<PhotoDto> AddAsync(int repairTicketId, string filePath, PhotoType photoType);
    Task<IEnumerable<PhotoDto>> GetByRepairTicketIdAsync(int repairTicketId);
    Task<bool> DeleteAsync(int photoId);
}
