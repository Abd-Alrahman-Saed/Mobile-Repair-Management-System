namespace MRMS.UI.Services;

public enum NotificationType
{
    Success,
    Error,
    Info,
    Warning
}

public class NotificationEventArgs(string message, NotificationType type) : EventArgs
{
    public string Message { get; } = message;
    public NotificationType Type { get; } = type;
}

public class ConfirmEventArgs(string message, TaskCompletionSource<bool> tcs) : EventArgs
{
    public string Message { get; } = message;
    public TaskCompletionSource<bool> Tcs { get; } = tcs;
}

public class NotificationService
{
    public event EventHandler<NotificationEventArgs>? OnNotification;
    public event EventHandler<ConfirmEventArgs>? OnConfirm;

    public void Show(string message, NotificationType type = NotificationType.Success)
    {
        OnNotification?.Invoke(this, new NotificationEventArgs(message, type));
    }

    public Task<bool> ConfirmAsync(string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        OnConfirm?.Invoke(this, new ConfirmEventArgs(message, tcs));
        return tcs.Task;
    }
}
