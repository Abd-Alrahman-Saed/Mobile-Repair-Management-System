namespace MRMS.Application.DTOs;

public record BackupSettingsDto(
    string BackupFolder,
    string EffectiveBackupFolder,
    int RetentionCount,
    DateOnly? LastBackupDate);

public record BackupFileInfoDto(
    string FileName,
    string FullPath,
    DateTime CreatedAt,
    long SizeBytes);

public record BackupHistoryEntryDto(
    DateTime Timestamp,
    string Action,
    bool Success,
    string FilePath,
    long SizeBytes,
    string? Message);

public record BackupResultDto(
    bool Success,
    string? FilePath,
    string? ErrorMessage,
    bool UsedFallbackFolder);

public record RestoreResultDto(
    bool Success,
    string? ErrorMessage,
    bool RestartRequired);
