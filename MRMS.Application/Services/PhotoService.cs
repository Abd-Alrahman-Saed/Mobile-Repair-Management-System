using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class PhotoService : IPhotoService
{
    private readonly IUnitOfWork _uow;

    public PhotoService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PhotoDto> AddAsync(int repairTicketId, string filePath, PhotoType photoType)
    {
        var photo = new DevicePhoto
        {
            RepairTicketId = repairTicketId,
            FilePath = filePath,
            PhotoType = photoType,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.DevicePhotos.AddAsync(photo);
        await _uow.CompleteAsync();
        return MapToDto(photo);
    }

    public async Task<IEnumerable<PhotoDto>> GetByRepairTicketIdAsync(int repairTicketId)
    {
        var photos = await _uow.DevicePhotos.FindAsync(
            p => p.RepairTicketId == repairTicketId);
        return photos.Select(MapToDto);
    }

    public async Task<bool> DeleteAsync(int photoId)
    {
        var photo = await _uow.DevicePhotos.GetByIdAsync(photoId);
        if (photo is null) return false;

        _uow.DevicePhotos.Delete(photo);
        await _uow.CompleteAsync();
        return true;
    }

    private static PhotoDto MapToDto(DevicePhoto p) =>
        new(p.Id, p.RepairTicketId, p.FilePath, p.PhotoType, p.CreatedAt);
}
