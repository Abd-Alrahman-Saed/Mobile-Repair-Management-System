using Microsoft.Data.SqlClient;

namespace MRMS.Infrastructure.Services;

/// <summary>
/// SQL Server-native backup/restore. Uses the server's own BACKUP DATABASE / RESTORE DATABASE /
/// RESTORE VERIFYONLY commands - a consistent, engine-managed snapshot rather than a raw file copy,
/// verified before being trusted.
///
/// IMPORTANT (verified live against a real SQL Server Express instance, not assumed): BACKUP/RESTORE
/// DATABASE are executed BY THE SQL SERVER WINDOWS SERVICE, not by this app's own process, so the path
/// passed to them must be one *that service account* can read/write - not just the interactive user.
/// Pointing BACKUP DATABASE directly at an arbitrary user folder (e.g. %AppData% or a USB drive) fails
/// with "Access is denied" even though the app itself could write there fine. The fix used here:
/// BACKUP/RESTORE always go through SQL Server's own default backup directory (queried via
/// SERVERPROPERTY('InstanceDefaultBackupPath'), which the service account can always read/write since
/// it's SQL Server's own data folder) as a staging location, and this app - running as the interactive
/// user - does a plain file copy between that staging folder and the caller-supplied destination path.
/// That plain copy is an ordinary file operation with no special permission needs.
/// </summary>
public class SqlServerBackupService : BackupServiceBase
{
    private readonly string _masterConnectionString;
    private readonly string _databaseName;
    private string? _stagingFolderCache;

    protected override string FileExtension => ".bak";

    public SqlServerBackupService(string connectionString) : base(connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        _databaseName = builder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        _masterConnectionString = masterBuilder.ConnectionString;
    }

    /// <summary>The folder SQL Server's own Windows service account can read/write - used as a staging
    /// area for every backup/restore, since only that account (not this app's process) can access it
    /// directly. Queried once from the server and cached.</summary>
    private async Task<string> GetStagingFolderAsync()
    {
        if (_stagingFolderCache is not null) return _stagingFolderCache;

        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000));", connection);
        var result = await cmd.ExecuteScalarAsync();

        var path = result as string;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("تعذر تحديد مجلد النسخ الاحتياطي الخاص بخادم SQL Server");

        _stagingFolderCache = path;
        return path;
    }

    protected override async Task BackupToPathAsync(string destPath)
    {
        var stagingFolder = await GetStagingFolderAsync();
        var stagingPath = Path.Combine(stagingFolder, $"staging-{Guid.NewGuid():N}.bak");

        try
        {
            await using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();
                try
                {
                    await ExecuteBackupCommandAsync(connection, stagingPath, withCompression: true);
                }
                catch (SqlException ex) when (IsCompressionUnsupported(ex))
                {
                    // SQL Server Express (and some other editions) don't support backup compression - retry without it.
                    await ExecuteBackupCommandAsync(connection, stagingPath, withCompression: false);
                }
            }

            if (!File.Exists(stagingPath))
                throw new InvalidOperationException("لم يتم إنشاء ملف النسخة الاحتياطية");

            await VerifyBackupFileAsync(stagingPath);
            File.Copy(stagingPath, destPath, overwrite: true);
        }
        finally
        {
            TryDelete(stagingPath);
        }
    }

    private async Task ExecuteBackupCommandAsync(SqlConnection connection, string path, bool withCompression)
    {
        var options = withCompression ? "INIT, CHECKSUM, COMPRESSION" : "INIT, CHECKSUM";
        await using var cmd = new SqlCommand(
            $"BACKUP DATABASE [{_databaseName}] TO DISK = @path WITH {options};", connection);
        cmd.Parameters.AddWithValue("@path", path);
        cmd.CommandTimeout = 300; // large DBs can take a while
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool IsCompressionUnsupported(SqlException ex) =>
        ex.Message.Contains("COMPRESSION", StringComparison.OrdinalIgnoreCase);

    protected override async Task VerifyBackupFileAsync(string path)
    {
        // The service account can only read from its own staging folder, not an arbitrary caller path -
        // copy there first if needed.
        var stagingFolder = await GetStagingFolderAsync();
        var isAlreadyStaged = Path.GetDirectoryName(path)?.Equals(stagingFolder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) == true;
        var verifyPath = isAlreadyStaged ? path : Path.Combine(stagingFolder, $"verify-{Guid.NewGuid():N}.bak");

        try
        {
            if (!isAlreadyStaged)
                File.Copy(path, verifyPath, overwrite: true);

            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            await using var cmd = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @path;", connection);
            cmd.Parameters.AddWithValue("@path", verifyPath);
            cmd.CommandTimeout = 300;

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"فشل التحقق من سلامة النسخة الاحتياطية: {ex.Message}", ex);
            }
        }
        finally
        {
            if (!isAlreadyStaged) TryDelete(verifyPath);
        }
    }

    protected override async Task ReplaceLiveDatabaseAsync(string verifiedSourcePath)
    {
        var stagingFolder = await GetStagingFolderAsync();
        var stagingSourcePath = Path.Combine(stagingFolder, $"restore-source-{Guid.NewGuid():N}.bak");

        try
        {
            File.Copy(verifiedSourcePath, stagingSourcePath, overwrite: true);

            SqlConnection.ClearAllPools();

            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            await using (var single = new SqlCommand(
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", connection))
            {
                single.CommandTimeout = 60;
                await single.ExecuteNonQueryAsync();
            }

            try
            {
                await using var restore = new SqlCommand(
                    $"RESTORE DATABASE [{_databaseName}] FROM DISK = @path WITH REPLACE;", connection);
                restore.Parameters.AddWithValue("@path", stagingSourcePath);
                restore.CommandTimeout = 600;
                await restore.ExecuteNonQueryAsync();
            }
            finally
            {
                await using var multi = new SqlCommand(
                    $"ALTER DATABASE [{_databaseName}] SET MULTI_USER;", connection);
                multi.CommandTimeout = 60;
                await multi.ExecuteNonQueryAsync();
            }

            SqlConnection.ClearAllPools();
        }
        finally
        {
            TryDelete(stagingSourcePath);
        }
    }
}
