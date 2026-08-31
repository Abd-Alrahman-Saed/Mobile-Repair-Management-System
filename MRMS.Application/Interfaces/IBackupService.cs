using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IBackupService
{
    /// <summary>Current backup settings (configured folder, effective folder actually in use, retention, last backup date).</summary>
    BackupSettingsDto GetSettings();

    /// <summary>Updates the configured backup folder and/or retention count. Pass null to leave a value unchanged.</summary>
    Task UpdateSettingsAsync(string? backupFolder, int? retentionCount);

    /// <summary>
    /// Runs a full backup now: BACKUP DATABASE, RESTORE VERIFYONLY check, retention cleanup, history log entry.
    /// Headless - no UI/dialog involved, safe to call from anywhere (button handler, scheduler, or future
    /// silent callers such as a pre-migration safety backup). Pass <paramref name="reason"/> to label the
    /// history entry with something more specific than "manual"/"automatic" (e.g. "before schema update").
    /// </summary>
    Task<BackupResultDto> BackupNowAsync(bool isAutomatic = false, string? reason = null);

    /// <summary>
    /// Restores the database from the given backup file: verifies it, takes a pre-restore safety backup of the
    /// current database, then replaces the live database. The app must be restarted afterward.
    /// </summary>
    Task<RestoreResultDto> RestoreAsync(string backupFilePath);

    /// <summary>Lists backup files (*.bak) in the effective backup folder, newest first.</summary>
    Task<IEnumerable<BackupFileInfoDto>> ListBackupsAsync();

    /// <summary>Backup/restore history log, newest first.</summary>
    Task<IEnumerable<BackupHistoryEntryDto>> GetHistoryAsync();

    /// <summary>True if no successful backup has been recorded for today and it's already past the scheduled time (12:00).</summary>
    bool IsScheduledBackupDue();

    /// <summary>Time remaining until the next scheduled backup (12:00 today, or 12:00 tomorrow if already past for today).</summary>
    TimeSpan GetTimeUntilScheduledBackup();
}
