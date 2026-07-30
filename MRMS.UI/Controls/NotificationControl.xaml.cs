using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MRMS.UI.Services;

namespace MRMS.UI.Controls;

public partial class NotificationControl : System.Windows.Controls.UserControl
{
    private readonly NotificationService _service;
    private TaskCompletionSource<bool>? _currentConfirmTcs;
    private readonly DispatcherTimer _hideTimer;

    public NotificationControl(NotificationService service)
    {
        InitializeComponent();
        _service = service;
        _service.OnNotification += OnNotificationReceived;
        _service.OnConfirm += OnConfirmRequested;

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideTimer.Tick += (_, _) => { _hideTimer.Stop(); HideToast(); };
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageText.Text = e.Message;
            ToastBorder.Tag = "Visible";

            IconText.Text = e.Type switch
            {
                NotificationType.Success => "✓",
                NotificationType.Error => "✕",
                NotificationType.Warning => "!",
                _ => "i"
            };

            ToastBorder.Background = e.Type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                NotificationType.Error => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                _ => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB))
            };

            _hideTimer.Stop();
            _hideTimer.Start();
        });
    }

    private void OnConfirmRequested(object? sender, ConfirmEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _currentConfirmTcs = e.Tcs;
            ConfirmText.Text = e.Message;
            Overlay.Visibility = Visibility.Visible;
        });
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        _currentConfirmTcs?.TrySetResult(true);
        _currentConfirmTcs = null;
        Overlay.Visibility = Visibility.Collapsed;
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        _currentConfirmTcs?.TrySetResult(false);
        _currentConfirmTcs = null;
        Overlay.Visibility = Visibility.Collapsed;
    }

    private void HideToast()
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.3) };
        Storyboard.SetTarget(animation, ToastBorder);
        Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => ToastBorder.Tag = null;
        storyboard.Begin();
    }

    private void ToastBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _hideTimer.Stop();
        HideToast();
    }

    private void Close_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _hideTimer.Stop();
        HideToast();
    }
}
