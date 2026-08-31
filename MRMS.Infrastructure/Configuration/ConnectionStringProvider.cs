using System.Text.Json;
using Serilog;

namespace MRMS.Infrastructure.Configuration;

public enum DatabaseProvider
{
    SqlServer,
    Sqlite
}

public record DatabaseConfig(DatabaseProvider Provider, string ConnectionString);

/// <summary>
/// Resolves which database engine and connection string to use from an external config file under
/// %AppData%\MRMS instead of anything baked into the compiled app - so it survives updates and can differ
/// per machine without a recompile.
///
/// Two real-world scenarios this supports:
///  - A dev/shop machine that already has SQL Server Express set up (e.g. this dev machine, connected to
///    "ABDO-DESKTOP\SQLEXPRESS") keeps working exactly as before - its config file already pins that
///    provider and connection string explicitly.
///  - A brand-new machine with no config file yet (a fresh self-contained publish run somewhere that has
///    never had this app installed - a test sandbox, a customer's PC) bootstraps itself with SQLite: a
///    single local file under %AppData%\MRMS\Data\app.db, no external database server required at all.
///
/// Old config files written before this file supported multiple providers only ever contained a SQL
/// Server-style connection string with no "Provider" field - those are treated as SqlServer for backward
/// compatibility (see LoadFromDisk), so an existing install is never silently reinterpreted as SQLite.
/// </summary>
public static class ConnectionStringProvider
{
    private sealed class ConnectionConfigModel
    {
        public string? Provider { get; set; }
        public string? ConnectionString { get; set; }
    }

    public static DatabaseConfig GetDatabaseConfig()
    {
        var configPath = GetConfigPath();

        var existing = TryLoadFromDisk(configPath);
        if (existing is not null)
            return existing;

        // First run: no config file yet. Bootstrap with SQLite so the app is fully self-contained -
        // no SQL Server installation required on this machine - and the file becomes the editable
        // source of truth going forward (e.g. to point an existing shop machine at SQL Server instead).
        return BootstrapSqliteDefault(configPath);
    }

    private static DatabaseConfig? TryLoadFromDisk(string configPath)
    {
        if (!File.Exists(configPath)) return null;

        try
        {
            var json = File.ReadAllText(configPath);
            var model = JsonSerializer.Deserialize<ConnectionConfigModel>(json);
            if (model is null || string.IsNullOrWhiteSpace(model.ConnectionString))
                return null;

            // No "Provider" field = a config file written before this app supported SQLite at all,
            // which only ever meant SQL Server. Never reinterpret an existing install's connection
            // string as something else.
            var provider = string.IsNullOrWhiteSpace(model.Provider)
                ? DatabaseProvider.SqlServer
                : Enum.TryParse<DatabaseProvider>(model.Provider, ignoreCase: true, out var parsed)
                    ? parsed
                    : DatabaseProvider.SqlServer;

            return new DatabaseConfig(provider, model.ConnectionString);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Connection config file at {Path} exists but could not be read - falling back to the default SQLite database for this session", configPath);
            return BuildSqliteConfig(configPath);
        }
    }

    private static DatabaseConfig BootstrapSqliteDefault(string configPath)
    {
        var config = BuildSqliteConfig(configPath);

        try
        {
            if (!File.Exists(configPath)) // don't clobber a file that appeared between the check and here
            {
                var json = JsonSerializer.Serialize(
                    new ConnectionConfigModel { Provider = nameof(DatabaseProvider.Sqlite), ConnectionString = config.ConnectionString },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);

                Log.Information(
                    "First run: created connection config at {Path} using a local SQLite database at {DbPath}. " +
                    "No SQL Server installation is required. Edit this file to switch to SQL Server instead.",
                    configPath, ExtractSqliteDataSource(config.ConnectionString));
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: the app still runs fine with this in-memory default even if we couldn't persist
            // it (e.g. no write permission) - just means it'll re-bootstrap the same way every launch.
            Log.Warning(ex, "Could not create connection config at {Path} - using the default SQLite database for this session", configPath);
        }

        return config;
    }

    private static DatabaseConfig BuildSqliteConfig(string configPath)
    {
        var dataDir = Path.Combine(Path.GetDirectoryName(configPath)!, "Data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "app.db");
        return new DatabaseConfig(DatabaseProvider.Sqlite, $"Data Source={dbPath}");
    }

    private static string ExtractSqliteDataSource(string connectionString)
    {
        const string prefix = "Data Source=";
        var idx = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        return idx < 0 ? connectionString : connectionString[(idx + prefix.Length)..].Split(';')[0];
    }

    private static string GetConfigPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MRMS");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "connection-config.json");
    }
}
