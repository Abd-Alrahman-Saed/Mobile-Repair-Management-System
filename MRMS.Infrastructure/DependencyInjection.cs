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
        // Scoped (the AddDbContext default) - not Transient. A single logical operation (e.g. a sale that
        // writes a Sale row, decrements Item stock, and posts a balanced journal entry) fans out across
        // several services: SaleService, JournalService, etc. If each got its own DbContext, one service's
        // SaveChanges would not persist another's pending inserts and the journal entry would silently
        // vanish. Scoped makes every service in the same resolution graph share one AppDbContext.
        // The WPF app has no per-operation scope, so resolving from the root provider yields one shared
        // context for the app lifetime; the background seed task in App.OnStartup opens its own scope and
        // is therefore isolated from the UI context.
        services.AddDbContext<AppDbContext>(options =>
        {
            if (databaseConfig.Provider == DatabaseProvider.Sqlite)
                options.UseSqlite(databaseConfig.ConnectionString);
            else
                options.UseSqlServer(databaseConfig.ConnectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient<SeedService>();

        services.AddSingleton<IBackupService>(_ => databaseConfig.Provider == DatabaseProvider.Sqlite
            ? new SqliteBackupService(databaseConfig.ConnectionString)
            : new SqlServerBackupService(databaseConfig.ConnectionString));

        return services;
    }
}
