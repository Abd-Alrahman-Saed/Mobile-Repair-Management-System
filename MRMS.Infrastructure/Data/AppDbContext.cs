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
    }
}
