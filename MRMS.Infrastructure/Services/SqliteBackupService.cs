using Microsoft.Data.Sqlite;

namespace MRMS.Infrastructure.Services;

/// <summary>
/// SQLite-native backup/restore, using SQLite's own safe mechanisms rather than a naive file copy (which
/// risks copying a file mid-write and producing a corrupt backup):
///  - Backup: `VACUUM INTO 'path'` executed against the live connection - produces a clean, compacted,
///    transactionally-consistent snapshot in one atomic step, even while the source database is in use.
///  - Verify: open the resulting file in its own connection and run `PRAGMA integrity_check`.
///
/// Unlike SQL Server, there's no separate database service process here - this app's own process owns the
/// database file directly, so it can read/write any folder the interactive user can (including a USB
/// drive) with no special permission workaround needed.
/// </summary>
public class SqliteBackupService : BackupServiceBase
{
    private readonly string _liveDbPath;

    protected override string FileExtension => ".db";

    public SqliteBackupService(string connectionString) : base(connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        _liveDbPath = builder.DataSource;
    }

    protected override async Task BackupToPathAsync(string destPath)
    {
        await using (var connection = new SqliteConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "VACUUM INTO $path;";
            cmd.Parameters.AddWithValue("$path", destPath);
            cmd.CommandTimeout = 300;
            await cmd.ExecuteNonQueryAsync();
        }

        if (!File.Exists(destPath))
            throw new InvalidOperationException("لم يتم إنشاء ملف النسخة الاحتياطية");

        await VerifyBackupFileAsync(destPath);
    }

    protected override async Task VerifyBackupFileAsync(string path)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly };
        await using var connection = new SqliteConnection(builder.ConnectionString);

        try
        {
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"تعذر فتح ملف النسخة الاحتياطية: {ex.Message}", ex);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        cmd.CommandTimeout = 300;

        string? result;
        try
        {
            result = (string?)await cmd.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"فشل التحقق من سلامة النسخة الاحتياطية: {ex.Message}", ex);
        }

        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"النسخة الاحتياطية غير سليمة: {result}");
    }

    protected override Task ReplaceLiveDatabaseAsync(string verifiedSourcePath)
    {
        // Release every pooled connection this process holds to the live file before overwriting it.
        SqliteConnection.ClearAllPools();

        File.Copy(verifiedSourcePath, _liveDbPath, overwrite: true);

        // Any -wal/-shm/-journal sidecar files left over belong to the OLD database content and must not
        // be replayed against the newly-restored file, or SQLite could see a mismatched/corrupt state.
        TryDelete(_liveDbPath + "-wal");
        TryDelete(_liveDbPath + "-shm");
        TryDelete(_liveDbPath + "-journal");

        return Task.CompletedTask;
    }
}
