using System.Text.Json;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.Infrastructure.Services;

/// <summary>
/// Everything about backup/restore that does NOT depend on which database engine is in use: where backup
/// files/settings/history live (all under %AppData%\MRMS, deliberately outside the database itself so a
/// restore can never erase the log entry documenting that very restore), retention trimming, and the
/// backup-now/restore-now orchestration. Subclasses supply only the provider-specific mechanics: how to
/// actually produce a verified backup file, how to verify an arbitrary file, and how to make a verified
/// file become the live database.
/// </summary>
public abstract class BackupServiceBase : IBackupService
{
    private const int DefaultRetentionCount = 30;
    private const int MaxHistoryEntries = 500;
    private static readonly TimeSpan ScheduledTime = new(12, 0, 0);

    protected readonly string ConnectionString;
    private readonly string _configPath;
    private readonly string _historyPath;
    private readonly string _defaultBackupFolder;

    /// <summary>File extension used for this provider's backup files, including the dot (e.g. ".bak", ".db").</summary>
    protected abstract string FileExtension { get; }

    protected BackupServiceBase(string connectionString)
    {
        ConnectionString = connectionString;

        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MRMS");
        Directory.CreateDirectory(appDataDir);

        _configPath = Path.Combine(appDataDir, "backup-config.json");
        _historyPath = Path.Combine(appDataDir, "backup-history.json");
        _defaultBackupFolder = Path.Combine(appDataDir, "Backups");
    }

    /// <summary>Creates a verified backup file at exactly <paramref name="destPath"/>.</summary>
    protected abstract Task BackupToPathAsync(string destPath);

    /// <summary>Verifies an arbitrary existing file is a valid, restorable backup. Throws if not.</summary>
    protected abstract Task VerifyBackupFileAsync(string path);

    /// <summary>Replaces the live database with the (already-verified) backup at <paramref name="verifiedSourcePath"/>.</summary>
    protected abstract Task ReplaceLiveDatabaseAsync(string verifiedSourcePath);

    // ===== Settings =====

    private sealed class ConfigModel
    {
        public string? BackupFolder { get; set; }
        public int RetentionCount { get; set; } = DefaultRetentionCount;
        public DateOnly? LastBackupDate { get; set; }
    }

    private ConfigModel LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var model = JsonSerializer.Deserialize<ConfigModel>(json);
                if (model is not null) return model;
            }
        }
        catch
        {
            // Corrupt/unreadable config file - fall back to defaults rather than crash.
        }
        return new ConfigModel();
    }

    private void SaveConfig(ConfigModel config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    /// <summary>The destination folder actually usable right now: the configured one if it exists/is
    /// writable by this app process, otherwise the default under %AppData%.</summary>
    private string ResolveEffectiveFolder(string? configuredFolder, out bool usedFallback)
    {
        usedFallback = false;
        if (!string.IsNullOrWhiteSpace(configuredFolder))
        {
            try
            {
                Directory.CreateDirectory(configuredFolder);
                var probe = Path.Combine(configuredFolder, $".write-check-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return configuredFolder;
            }
            catch
            {
                usedFallback = true;
            }
        }

        Directory.CreateDirectory(_defaultBackupFolder);
        return _defaultBackupFolder;
    }

    public BackupSettingsDto GetSettings()
    {
        var config = LoadConfig();
        var effective = ResolveEffectiveFolder(config.BackupFolder, out _);
        return new BackupSettingsDto(config.BackupFolder ?? "", effective, config.RetentionCount, config.LastBackupDate);
    }

    public Task UpdateSettingsAsync(string? backupFolder, int? retentionCount)
    {
        var config = LoadConfig();
        if (backupFolder is not null) config.BackupFolder = backupFolder;
        if (retentionCount is not null) config.RetentionCount = Math.Max(1, retentionCount.Value);
        SaveConfig(config);
        return Task.CompletedTask;
    }

    // ===== History =====

    public Task<IEnumerable<BackupHistoryEntryDto>> GetHistoryAsync()
    {
        var entries = LoadHistory();
        entries.Reverse(); // newest first
        return Task.FromResult<IEnumerable<BackupHistoryEntryDto>>(entries);
    }

    private List<BackupHistoryEntryDto> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                var list = JsonSerializer.Deserialize<List<BackupHistoryEntryDto>>(json);
                if (list is not null) return list;
            }
        }
        catch
        {
            // Corrupt history file - start fresh rather than crash the app over a log.
        }
        return [];
    }

    private void AppendHistory(string action, bool success, string filePath, long sizeBytes, string? message)
    {
        try
        {
            var entries = LoadHistory();
            entries.Add(new BackupHistoryEntryDto(DateTime.Now, action, success, filePath, sizeBytes, message));
            if (entries.Count > MaxHistoryEntries)
                entries = entries.Skip(entries.Count - MaxHistoryEntries).ToList();

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }
        catch
        {
            // Never let a logging failure block/crash a backup or restore that otherwise succeeded.
        }
    }

    // ===== Backup =====

    public async Task<BackupResultDto> BackupNowAsync(bool isAutomatic = false, string? reason = null)
    {
        var actionLabel = reason ?? (isAutomatic ? "نسخ احتياطي تلقائي" : "نسخ احتياطي يدوي");

        var config = LoadConfig();
        var destFolder = ResolveEffectiveFolder(config.BackupFolder, out var usedFallback);
        var fileName = $"MRMS-backup-{DateTime.Now:yyyy-MM-dd_HHmm}{FileExtension}";
        var destPath = Path.Combine(destFolder, fileName);

        try
        {
            await BackupToPathAsync(destPath);

            var sizeBytes = new FileInfo(destPath).Length;

            config.LastBackupDate = DateOnly.FromDateTime(DateTime.Now);
            SaveConfig(config);

            TrimRetention(destFolder, config.RetentionCount);

            AppendHistory(actionLabel, true, destPath, sizeBytes, null);

            return new BackupResultDto(true, destPath, null, usedFallback);
        }
        catch (Exception ex)
        {
            TryDelete(destPath);
            AppendHistory(actionLabel, false, destPath, 0, ex.Message);
            return new BackupResultDto(false, null, ex.Message, usedFallback);
        }
    }

    private void TrimRetention(string folder, int retentionCount)
    {
        var keep = Math.Max(1, retentionCount);
        var files = new DirectoryInfo(folder)
            .GetFiles($"*{FileExtension}")
            .OrderByDescending(f => f.CreationTime)
            .ToList();

        foreach (var file in files.Skip(keep))
            TryDelete(file.FullName);
    }

    protected static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }

    public Task<IEnumerable<BackupFileInfoDto>> ListBackupsAsync()
    {
        var config = LoadConfig();
        var folder = ResolveEffectiveFolder(config.BackupFolder, out _);

        if (!Directory.Exists(folder))
            return Task.FromResult(Enumerable.Empty<BackupFileInfoDto>());

        var files = new DirectoryInfo(folder)
            .GetFiles($"*{FileExtension}")
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupFileInfoDto(f.Name, f.FullName, f.CreationTime, f.Length));

        return Task.FromResult(files);
    }

    // ===== Restore =====

    public async Task<RestoreResultDto> RestoreAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            return new RestoreResultDto(false, "ملف النسخة الاحتياطية غير موجود", false);

        try
        {
            await VerifyBackupFileAsync(backupFilePath);
        }
        catch (Exception ex)
        {
            AppendHistory("استرجاع (مرفوض)", false, backupFilePath, 0, ex.Message);
            return new RestoreResultDto(false, $"ملف النسخة الاحتياطية غير سليم ولا يمكن الاسترجاع منه: {ex.Message}", false);
        }

        // Take a safety backup of the CURRENT database first, so a wrong choice can be undone.
        var config = LoadConfig();
        var destFolder = ResolveEffectiveFolder(config.BackupFolder, out _);
        var safetyPath = Path.Combine(destFolder, $"MRMS-pre-restore-safety-{DateTime.Now:yyyy-MM-dd_HHmm}{FileExtension}");

        try
        {
            await BackupToPathAsync(safetyPath);
            AppendHistory("نسخة أمان قبل الاسترجاع", true, safetyPath, new FileInfo(safetyPath).Length, null);
        }
        catch (Exception ex)
        {
            TryDelete(safetyPath);
            AppendHistory("نسخة أمان قبل الاسترجاع", false, safetyPath, 0, ex.Message);
            return new RestoreResultDto(false, $"تعذر أخذ نسخة أمان للبيانات الحالية قبل الاسترجاع، تم إلغاء العملية لحمايتك: {ex.Message}", false);
        }

        try
        {
            await ReplaceLiveDatabaseAsync(backupFilePath);
            AppendHistory("استرجاع", true, backupFilePath, new FileInfo(backupFilePath).Length, null);
            return new RestoreResultDto(true, null, true);
        }
        catch (Exception ex)
        {
            AppendHistory("استرجاع", false, backupFilePath, 0, ex.Message);
            return new RestoreResultDto(false,
                $"فشل الاسترجاع: {ex.Message}. تم أخذ نسخة أمان قبل المحاولة في: {safetyPath}", false);
        }
    }

    // ===== Scheduling helpers =====

    public bool IsScheduledBackupDue()
    {
        var config = LoadConfig();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var alreadyDoneToday = config.LastBackupDate == today;
        return !alreadyDoneToday && DateTime.Now.TimeOfDay >= ScheduledTime;
    }

    public TimeSpan GetTimeUntilScheduledBackup()
    {
        var now = DateTime.Now;
        var todayNoon = now.Date.Add(ScheduledTime);
        var target = now < todayNoon ? todayNoon : todayNoon.AddDays(1);
        return target - now;
    }
}
