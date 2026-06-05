using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class CustomerFormViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly NotificationService _notificationService;
    private int? _editId;

    [ObservableProperty]
    private string _fullName = "";

    [ObservableProperty]
    private string _phoneNumber = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditMode;

    public event Action? SaveCompleted;

    public CustomerFormViewModel(ICustomerService customerService, NotificationService notificationService)
    {
        _customerService = customerService;
        _notificationService = notificationService;
    }

    public async Task LoadForEditAsync(int id)
    {
        _editId = id;
        IsEditMode = true;
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is not null)
        {
            FullName = customer.FullName;
            PhoneNumber = customer.PhoneNumber;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            if (_editId.HasValue)
            {
                await _customerService.UpdateAsync(
                    new UpdateCustomerDto(_editId.Value, FullName, PhoneNumber));
                _notificationService.Show("تم حفظ البيانات بنجاح", NotificationType.Success);
            }
            else
            {
                await _customerService.CreateAsync(
                    new CreateCustomerDto(FullName, PhoneNumber));
                _notificationService.Show("تم إضافة العميل بنجاح", NotificationType.Success);
            }
            SaveCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
