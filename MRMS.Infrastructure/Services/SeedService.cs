using Microsoft.EntityFrameworkCore;
using MRMS.Application.Security;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
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

        await SeedOwnerAsync();

        if (!await _context.SystemSettings.AnyAsync())
        {
            _context.SystemSettings.AddRange(
                new SystemSetting { Key = "PaymentMethods", Value = "نقدي,آجل" },
                new SystemSetting { Key = "ExpenseCategories", Value = "إيجار,كهرباء,رواتب,إنترنت,مواصلات,صيانة,أخرى" }
            );
            await _context.SaveChangesAsync();
        }

        // Discount ceilings by role (percent). Added idempotently so existing installs pick them up too.
        await EnsureSettingAsync("Discount.MaxPercent.Receptionist", "0");
        await EnsureSettingAsync("Discount.MaxPercent.Manager", "15");
        await EnsureSettingAsync("Alerts.StaleTicketDays", "3");
        await EnsureSettingAsync("Notify.MessageTemplate",
            "مرحباً، تذكرة الصيانة رقم {TicketNumber} الخاصة بجهاز {Device} أصبحت جاهزة للاستلام. المبلغ المستحق: {Amount} جنيه. شكراً لثقتكم.");
    }

    private async Task EnsureSettingAsync(string key, string value)
    {
        if (!await _context.SystemSettings.AnyAsync(s => s.Key == key))
        {
            _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// A shop must have exactly one usable Owner account to start. Fresh install: create "owner" with a
    /// throwaway password that must be changed on first login (no other default password lives in code).
    /// Upgrade from the old single-password model: promote the oldest existing account to Owner.
    /// </summary>
    private async Task SeedOwnerAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Role == UserRole.Owner))
            return;

        var oldest = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        if (oldest is not null)
        {
            oldest.Role = UserRole.Owner;
            oldest.IsActive = true;
            if (string.IsNullOrWhiteSpace(oldest.FullName))
                oldest.FullName = "المالك";
            await _context.SaveChangesAsync();
            return;
        }

        var (hash, salt) = PasswordHasher.Hash("owner");
        _context.Users.Add(new User
        {
            Username = "owner",
            FullName = "المالك",
            Role = UserRole.Owner,
            IsActive = true,
            PasswordHash = hash,
            PasswordSalt = salt,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
