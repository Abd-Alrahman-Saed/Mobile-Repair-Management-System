using System.Windows.Threading;
using MRMS.Application.Interfaces;
using Serilog;

namespace MRMS.UI.Services;

/// <summary>
/// Drives the "automatic daily backup at 12:00" requirement without a background Windows service:
/// on startup, catches up on a missed noon if the app opens later the same day; otherwise arms an
/// in-app timer for the next 12:00 boundary while the app stays open. If the app is never opened on a
/// given day, that day simply has no backup - acceptable for a single-user local app.
/// </summary>
public class BackupSchedulerService
{
    private readonly IBackupService _backupService;
    private readonly NotificationService _notificationService;
    private DispatcherTimer? _timer;
    private bool _started;

    public BackupSchedulerService(IBackupService backupService, NotificationService notificationService)
    {
        _backupService = backupService;
        _notificationService = notificationService;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        if (_backupService.IsScheduledBackupDue())
        {
            // App opened after 12:00 with no backup taken yet today - catch up immediately in the background.
            _ = RunScheduledBackupAsync();
        }

        ArmTimer();
    }

    private void ArmTimer()
    {
        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = _backupService.GetTimeUntilScheduledBackup()
        };
        _timer.Tick += async (_, _) =>
        {
            _timer!.Stop();
            if (_backupService.IsScheduledBackupDue())
                await RunScheduledBackupAsync();
            ArmTimer(); // re-arm for the following day, as long as the app keeps running
        };
        _timer.Start();
    }

    private async Task RunScheduledBackupAsync()
    {
        try
        {
            var result = await _backupService.BackupNowAsync(isAutomatic: true);
            if (result.Success)
            {
                var message = result.UsedFallbackFolder
                    ? "تم أخذ نسخة احتياطية تلقائية (تم استخدام المجلد الاحتياطي لأن المجلد المحدد غير متاح)"
                    : "تم أخذ نسخة احتياطية تلقائية بنجاح";
                _notificationService.Show(message, result.UsedFallbackFolder ? NotificationType.Warning : NotificationType.Success);
            }
            else
            {
                _notificationService.Show($"فشل أخذ النسخة الاحتياطية التلقائية: {result.ErrorMessage}", NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Scheduled backup failed unexpectedly");
            _notificationService.Show("حدث خطأ غير متوقع أثناء أخذ النسخة الاحتياطية التلقائية", NotificationType.Error);
        }
    }
}
