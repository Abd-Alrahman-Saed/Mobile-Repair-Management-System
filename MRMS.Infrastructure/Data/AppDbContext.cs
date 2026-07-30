using Microsoft.EntityFrameworkCore;
using MRMS.Core.Entities;

namespace MRMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();
    public DbSet<DevicePhoto> DevicePhotos => Set<DevicePhoto>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<RepairTicketItem> RepairTicketItems => Set<RepairTicketItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.PasswordHash).HasMaxLength(200);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.FullName).HasMaxLength(100);
            e.Property(c => c.PhoneNumber).HasMaxLength(20);
            e.HasIndex(c => c.PhoneNumber);
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.Property(d => d.IMEI).HasMaxLength(15);
            e.Property(d => d.Brand).HasMaxLength(50);
            e.Property(d => d.Model).HasMaxLength(50);
            e.Property(d => d.Color).HasMaxLength(30);
            e.HasIndex(d => d.IMEI);
            e.HasOne(d => d.Customer)
                .WithMany(c => c.Devices)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairTicket>(e =>
        {
            e.Property(t => t.TicketNumber).HasMaxLength(20);
            e.Property(t => t.ProblemDescription).HasMaxLength(1000);
            e.Property(t => t.ReceptionNotes).HasMaxLength(1000);
            e.Property(t => t.RepairDescription).HasMaxLength(1000);
            e.Property(t => t.Cost).HasColumnType("decimal(18,2)");
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasOne(t => t.Device)
                .WithMany(d => d.RepairTickets)
                .HasForeignKey(t => t.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DevicePhoto>(e =>
        {
            e.Property(p => p.FilePath).HasMaxLength(500);
            e.Property(p => p.PhotoType).HasConversion<string>().HasMaxLength(10);
            e.HasOne(p => p.RepairTicket)
                .WithMany(t => t.Photos)
                .HasForeignKey(p => p.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Warranty>(e =>
        {
            e.Property(w => w.DurationDays).HasColumnType("int");
            e.HasOne(w => w.RepairTicket)
                .WithOne(t => t.Warranty)
                .HasForeignKey<Warranty>(w => w.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.Property(i => i.Code).HasMaxLength(50);
            e.Property(i => i.Name).HasMaxLength(200);
            e.Property(i => i.Unit).HasMaxLength(20);
            e.Property(i => i.CostPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.SellingPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(i => i.Code).IsUnique();
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(s => s.Code).HasMaxLength(50);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.Phone).HasMaxLength(50);
            e.Property(s => s.OpeningBalance).HasColumnType("decimal(18,2)");
            e.Property(s => s.Notes).HasMaxLength(500);
            e.HasIndex(s => s.Code).IsUnique();
        });

        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(500);
            e.HasIndex(s => s.Key).IsUnique();
        });

        modelBuilder.Entity<RepairTicketItem>(e =>
        {
            e.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            e.HasOne(i => i.RepairTicket)
                .WithMany(t => t.RepairTicketItems)
                .HasForeignKey(i => i.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Item)
                .WithMany()
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
