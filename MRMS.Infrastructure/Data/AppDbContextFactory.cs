using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MRMS.Infrastructure.Configuration;

namespace MRMS.Infrastructure.Data;

/// <summary>
/// Used only by EF Core design-time tooling (e.g. `dotnet ef migrations add`), not by the running app.
/// Reads the same %AppData%\MRMS\connection-config.json as the app itself for consistency, so tooling
/// targets whichever provider/database the app is actually configured to use on this machine.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = ConnectionStringProvider.GetDatabaseConfig();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        if (config.Provider == DatabaseProvider.Sqlite)
            optionsBuilder.UseSqlite(config.ConnectionString);
        else
            optionsBuilder.UseSqlServer(config.ConnectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
