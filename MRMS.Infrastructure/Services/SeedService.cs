using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;

namespace MRMS.Infrastructure.Services;

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
        }

        if (!await _context.Customers.AnyAsync())
        {
            var customer1 = new Customer
            {
                FullName = "أحمد محمد",
                PhoneNumber = "0555123456",
                CreatedAt = DateTime.UtcNow
            };
            var customer2 = new Customer
            {
                FullName = "سارة خالد",
                PhoneNumber = "0555987654",
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.AddRange(customer1, customer2);
            await _context.SaveChangesAsync();

            var device1 = new Device
            {
                IMEI = "351234567891234",
                Brand = "Samsung",
                Model = "Galaxy S24",
                Color = "أسود",
                CustomerId = customer1.Id,
                CreatedAt = DateTime.UtcNow
            };
            var device2 = new Device
            {
                IMEI = "359876543210987",
                Brand = "iPhone",
                Model = "15 Pro",
                Color = "أبيض",
                CustomerId = customer2.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Devices.AddRange(device1, device2);
            await _context.SaveChangesAsync();

            _context.RepairTickets.Add(new RepairTicket
            {
                TicketNumber = "RPR-00001",
                DeviceId = device1.Id,
                ProblemDescription = "الشاشة مكسورة",
                Status = TicketStatus.Received,
                ReceiveDate = DateTime.UtcNow.Date,
                ReceiveTime = DateTime.UtcNow.TimeOfDay,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        if (!await _context.Items.AnyAsync())
        {
            _context.Items.AddRange(
                new Item { Code = "CH-001", Name = "شاحن سريع 20W", Type = ItemType.Accessory, Unit = "قطعة", CostPrice = 25, SellingPrice = 45, ReorderLevel = 10, CurrentStock = 50 },
                new Item { Code = "CB-001", Name = "كابل USB-C", Type = ItemType.Accessory, Unit = "قطعة", CostPrice = 8, SellingPrice = 18, ReorderLevel = 20, CurrentStock = 100 },
                new Item { Code = "SC-001", Name = "شاشة جلاس سامسونج S24", Type = ItemType.SparePart, Unit = "قطعة", CostPrice = 180, SellingPrice = 320, ReorderLevel = 3, CurrentStock = 5 },
                new Item { Code = "BT-001", Name = "بطارية ايفون 15 برو", Type = ItemType.SparePart, Unit = "قطعة", CostPrice = 120, SellingPrice = 250, ReorderLevel = 3, CurrentStock = 4 }
            );
            await _context.SaveChangesAsync();
        }

        if (!await _context.Suppliers.AnyAsync())
        {
            _context.Suppliers.AddRange(
                new Supplier { Code = "SUP-001", Name = "مؤسسة الإلكترونيات الحديثة", Phone = "0555000111", OpeningBalance = 0, Notes = "مورد رئيسي للإكسسوارات" },
                new Supplier { Code = "SUP-002", Name = "شركة قطع الغيار المتكاملة", Phone = "0555000222", OpeningBalance = 1500, Notes = "مورد قطع غيار الصيانة" }
            );
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
