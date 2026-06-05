using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MRMS.UI.Services;

namespace MRMS.UI.Controls;

public partial class NotificationControl : UserControl
{
    private readonly NotificationService _notificationService;

    public NotificationControl(NotificationService notificationService)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _notificationService.OnNotification += OnNotificationReceived;
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        Dispatcher.Invoke(() => Show(e.Message, e.Type));
    }

    private void Show(string message, NotificationType type)
    {
        ToastBorder.Visibility = Visibility.Visible;

        var (icon, bgColor) = type switch
        {
            NotificationType.Success => ("✓", "#10B981"),
            NotificationType.Error => ("✕", "#EF4444"),
            NotificationType.Info => ("ℹ", "#3B82F6"),
            NotificationType.Warning => ("⚠", "#F59E0B"),
            _ => ("✓", "#10B981")
        };

        IconText.Text = icon;
        MessageText.Text = message;
        ToastBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor));

        var opacityAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.25)
        };
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
        var sb = new Storyboard();
        sb.Children.Add(opacityAnim);
        sb.Begin(ToastBorder);

        _ = AutoHideAsync();
    }

    public void Hide()
    {
        var opacityAnim = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.2)
        };
        opacityAnim.Completed += (_, _) =>
        {
            ToastBorder.Visibility = Visibility.Collapsed;
            ToastBorder.Opacity = 0;
        };
        var sb = new Storyboard();
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
        sb.Children.Add(opacityAnim);
        sb.Begin(ToastBorder);
    }

    private async Task AutoHideAsync()
    {
        await Task.Delay(3000);
        Dispatcher.Invoke(Hide);
    }

    private void ToastBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Hide();
    }

    private void Close_Click(object sender, MouseButtonEventArgs e)
    {
        Hide();
    }
}
