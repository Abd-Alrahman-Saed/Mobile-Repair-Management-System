using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.Interfaces;
using MRMS.Application.Services;

namespace MRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<IDeviceService, DeviceService>();
        services.AddTransient<IRepairTicketService, RepairTicketService>();
        services.AddTransient<ISearchService, SearchService>();
        services.AddTransient<IWarrantyService, WarrantyService>();
        services.AddTransient<IPhotoService, PhotoService>();
        services.AddTransient<IItemService, ItemService>();
        services.AddTransient<ISupplierService, SupplierService>();
        services.AddTransient<ISystemSettingService, SystemSettingService>();
        services.AddTransient<IPurchaseService, PurchaseService>();
        services.AddTransient<ISaleService, SaleService>();
        services.AddTransient<IExpenseService, ExpenseService>();
        services.AddTransient<ISupplierPaymentService, SupplierPaymentService>();
        services.AddTransient<IStockMovementService, StockMovementService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IIncomeStatementService, IncomeStatementService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddTransient<IMonthlyReportService, MonthlyReportService>();

        return services;
    }
}
