using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.Interfaces;
using MRMS.Core.Interfaces;
using MRMS.Infrastructure.Configuration;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;
using MRMS.Infrastructure.Services;

namespace MRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, DatabaseConfig databaseConfig)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (databaseConfig.Provider == DatabaseProvider.Sqlite)
                options.UseSqlite(databaseConfig.ConnectionString);
            else
                options.UseSqlServer(databaseConfig.ConnectionString);
        }, ServiceLifetime.Transient);

        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient<SeedService>();

        services.AddSingleton<IBackupService>(_ => databaseConfig.Provider == DatabaseProvider.Sqlite
            ? new SqliteBackupService(databaseConfig.ConnectionString)
            : new SqlServerBackupService(databaseConfig.ConnectionString));

        return services;
    }
}
