using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MRMS.Core.Entities;
using MRMS.Infrastructure.Data;

namespace MRMS.Infrastructure.Services;

/// <summary>
/// Runs on every startup. Creates the database/schema if it doesn't exist yet (fresh install / fresh
/// machine), and seeds only the operational baseline a brand-new install needs to be usable: the login
/// user and the default dropdown options (payment methods, expense categories). Deliberately does NOT
/// seed any sample customers/devices/tickets/items/suppliers - a fresh deployment should open with real,
/// empty business data, not demo records left over from development.
/// </summary>
public class SeedService
{
    private readonly AppDbContext _context;

    public SeedService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // MigrateAsync - not EnsureCreatedAsync - for SQL Server: EnsureCreated builds the schema from the
        // current model once and never touches __EFMigrationsHistory, so a column/table added in a later
        // release would never reach an already-installed copy. MigrateAsync applies every pending migration
        // in order and is a no-op when the database is already current.
        // The migrations under Migrations/ are generated for the SQL Server provider only. Until a parallel
        // SQLite migration set exists (planned for the packaged build), the self-contained SQLite path keeps
        // using EnsureCreated so a fresh install still gets a working schema.
        if (_context.Database.IsRelational() && !_context.Database.IsSqlite())
            await _context.Database.MigrateAsync();
        else
            await _context.Database.EnsureCreatedAsync();

        if (!await _context.Users.AnyAsync())
        {
            var passwordHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes("admin123")));

            _context.Users.Add(new User
            {
                Username = "tech",
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        if (!await _context.SystemSettings.AnyAsync())
        {
            _context.SystemSettings.AddRange(
                new SystemSetting { Key = "PaymentMethods", Value = "نقدي,آجل" },
                new SystemSetting { Key = "ExpenseCategories", Value = "إيجار,كهرباء,رواتب,إنترنت,مواصلات,صيانة,أخرى" }
            );
            await _context.SaveChangesAsync();
        }
    }
}
