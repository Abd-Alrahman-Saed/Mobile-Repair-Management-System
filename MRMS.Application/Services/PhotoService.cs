using System.IO;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class PhotoService : IPhotoService
{
    private readonly IUnitOfWork _uow;

    // Device photos are the only record of a device's condition at intake. The old code stored the path
    // the user picked in the file dialog (e.g. something on their Desktop); the moment that file was
    // moved or deleted the photo was gone. Now the file is copied into an app-managed folder and only a
    // path relative to that folder is persisted, so the DB stays portable across machines.
    private static string PhotosRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MRMS", "Photos");

    public PhotoService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PhotoDto> AddAsync(int repairTicketId, string filePath, PhotoType photoType)
    {
        var storedPath = ImportFile(repairTicketId, filePath);

        var photo = new DevicePhoto
        {
            RepairTicketId = repairTicketId,
            FilePath = storedPath,
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

    /// <summary>
    /// Copies the picked file into %AppData%\MRMS\Photos\{ticketId}\ under a fresh GUID name and returns
    /// the path to persist - relative to PhotosRoot, using '/' so it round-trips across machines. If the
    /// source can't be copied (e.g. already gone), the original path is stored as-is so nothing is lost.
    /// </summary>
    private static string ImportFile(int repairTicketId, string sourcePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return sourcePath;

            var targetDir = Path.Combine(PhotosRoot, repairTicketId.ToString());
            Directory.CreateDirectory(targetDir);

            var ext = Path.GetExtension(sourcePath);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            File.Copy(sourcePath, Path.Combine(targetDir, fileName), overwrite: false);

            return $"{repairTicketId}/{fileName}";
        }
        catch
        {
            return sourcePath;
        }
    }

    /// <summary>Resolves a stored path back to an absolute one. Legacy rows hold an absolute path already.</summary>
    private static string ResolvePath(string storedPath) =>
        string.IsNullOrWhiteSpace(storedPath) || Path.IsPathRooted(storedPath)
            ? storedPath
            : Path.Combine(PhotosRoot, storedPath.Replace('/', Path.DirectorySeparatorChar));

    private static PhotoDto MapToDto(DevicePhoto p) =>
        new(p.Id, p.RepairTicketId, ResolvePath(p.FilePath), p.PhotoType, p.CreatedAt);
}
