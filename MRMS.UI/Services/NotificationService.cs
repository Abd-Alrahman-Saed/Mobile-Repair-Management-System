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

public class NotificationService
{
    public event EventHandler<NotificationEventArgs>? OnNotification;

    public void Show(string message, NotificationType type = NotificationType.Success)
    {
        OnNotification?.Invoke(this, new NotificationEventArgs(message, type));
    }
}
