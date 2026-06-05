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

        return services;
    }
}
