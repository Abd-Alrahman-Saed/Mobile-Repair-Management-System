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
