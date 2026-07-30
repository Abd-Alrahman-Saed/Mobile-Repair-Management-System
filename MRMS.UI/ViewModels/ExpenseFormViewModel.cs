using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class ExpenseFormViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _category = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public ExpenseFormViewModel(
        IExpenseService expenseService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _expenseService = expenseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "التصنيف مطلوب";
            return;
        }
        if (Amount <= 0)
        {
            ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreateExpenseDto(Date, Category, Description, Amount, SelectedPaymentMethod, Notes);
            await _expenseService.CreateAsync(dto);
            _notificationService.Show("تم إضافة المصروف بنجاح", NotificationType.Success);
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

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
