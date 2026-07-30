using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SupplierFormViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private int _editId;

    [ObservableProperty]
    private string _code = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _phone = "";

    [ObservableProperty]
    private decimal _openingBalance;

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditMode;

    public SupplierFormViewModel(ISupplierService supplierService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _supplierService = supplierService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    public void SetEditData(SupplierDto supplier)
    {
        _editId = supplier.Id;
        IsEditMode = true;
        Code = supplier.Code;
        Name = supplier.Name;
        Phone = supplier.Phone;
        OpeningBalance = supplier.OpeningBalance;
        Notes = supplier.Notes;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "اسم المورد مطلوب";
            return;
        }
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "كود المورد مطلوب";
            return;
        }

        IsLoading = true;
        try
        {
            if (IsEditMode)
            {
                await _supplierService.UpdateAsync(new UpdateSupplierDto(
                    _editId, Code, Name, Phone, OpeningBalance, Notes));
                _notificationService.Show("تم تحديث المورد بنجاح", NotificationType.Success);
            }
            else
            {
                await _supplierService.CreateAsync(new CreateSupplierDto(
                    Code, Name, Phone, OpeningBalance, Notes));
                _notificationService.Show("تم إضافة المورد بنجاح", NotificationType.Success);
            }
            _navigationService.GoBack();
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
