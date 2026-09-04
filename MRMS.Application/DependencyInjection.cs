using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.Interfaces;
using MRMS.Application.Services;

namespace MRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Session identity + authorization. CurrentUserService and PermissionService are stateless-ish
        // singletons (one signed-in user per process); AuditService writes through the request's UoW.
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IAlertService, AlertService>();
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
        services.AddTransient<IJournalService, JournalService>();

        return services;
    }
}
