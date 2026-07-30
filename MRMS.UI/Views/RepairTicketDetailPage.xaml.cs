using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class RepairTicketDetailPage : Page
{
    private readonly RepairTicketViewModel _vm;
    private readonly AppNavigationService _navigationService;
    private readonly IPhotoService _photoService;
    private readonly IWarrantyService _warrantyService;
    private readonly NotificationService _notificationService;

    public RepairTicketDetailPage(
        RepairTicketViewModel vm,
        AppNavigationService navigationService,
        IPhotoService photoService,
        IWarrantyService warrantyService,
        NotificationService notificationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        _photoService = photoService;
        _warrantyService = warrantyService;
        _notificationService = notificationService;
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            var ticketId = _navigationService.GetParameter<int?>("TicketId");
            if (ticketId.HasValue)
            {
                var ticket = await _repairTicketService.GetByIdAsync(ticketId.Value);
                if (ticket is not null)
                    _vm.SelectedTicket = ticket;
                await LoadPhotosAndWarrantyAsync(ticketId.Value);
                await _vm.LoadItemsCommand.ExecuteAsync(null);
            }
        };
    }

    private IRepairTicketService _repairTicketService =>
        App.ServiceProvider!.GetRequiredService<IRepairTicketService>();

    private async Task LoadPhotosAndWarrantyAsync(int ticketId)
    {
        var photos = await _photoService.GetByRepairTicketIdAsync(ticketId);
        _vm.Photos = new System.Collections.ObjectModel.ObservableCollection<PhotoDto>(photos);

        var warranty = await _warrantyService.GetByRepairTicketIdAsync(ticketId);
        if (warranty is not null)
            _vm.WarrantyInfo = $"الضمان ساري حتى: {warranty.ExpiryDate:yyyy-MM-dd}";
    }

    private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Image image && image.DataContext is PhotoDto photo)
        {
            if (File.Exists(photo.FilePath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(photo.FilePath) { UseShellExecute = true });
        }
    }

    private async void AddPhotoBefore(object sender, System.Windows.RoutedEventArgs e)
        => await AddPhoto(PhotoType.Before);

    private async void AddPhotoAfter(object sender, System.Windows.RoutedEventArgs e)
        => await AddPhoto(PhotoType.After);

    private async Task AddPhoto(PhotoType type)
    {
        if (_vm.SelectedTicket is null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "صور|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "اختر صورة"
        };
        if (dialog.ShowDialog() == true)
        {
            var photo = await _photoService.AddAsync(_vm.SelectedTicket.Id, dialog.FileName, type);
            _vm.Photos.Add(photo);
        }
    }

    private async void AssignWarranty(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_vm.SelectedTicket is null) return;
        if (_vm.WarrantyDuration <= 0)
        {
            _vm.ErrorMessage = "يرجى إدخال مدة ضمان صحيحة";
            return;
        }
        var dto = new AssignWarrantyDto(
            _vm.SelectedTicket.Id,
            _vm.WarrantyDuration,
            DateTime.UtcNow.AddDays(_vm.WarrantyDuration));
        var warranty = await _warrantyService.AssignAsync(dto);
        _vm.WarrantyInfo = $"الضمان ساري حتى: {warranty.ExpiryDate:yyyy-MM-dd}";
        _notificationService.Show("تم تعيين الضمان بنجاح", NotificationType.Success);
    }

    private void PrintInvoice(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_vm.SelectedTicket is null) return;
        var doc = PrintPreviewWindow.BuildInvoice(_vm.SelectedTicket, _vm.WarrantyInfo);
        var preview = new PrintPreviewWindow(doc);
        preview.ShowDialog();
    }

    private void PrintReceptionReceipt(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_vm.SelectedTicket is null) return;
        var doc = PrintPreviewWindow.BuildReceptionReceipt(_vm.SelectedTicket);
        var preview = new PrintPreviewWindow(doc);
        preview.ShowDialog();
    }
}
