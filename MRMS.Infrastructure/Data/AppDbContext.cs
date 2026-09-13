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
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<RepairTicketPayment> RepairTicketPayments => Set<RepairTicketPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.PasswordHash).HasMaxLength(200);
            e.Property(u => u.PasswordSalt).HasMaxLength(200);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.UserName).HasMaxLength(100);
            e.Property(a => a.Action).HasMaxLength(60);
            e.Property(a => a.EntityType).HasMaxLength(60);
            e.Property(a => a.Details).HasMaxLength(1000);
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.UserId);
            // UserId is a soft reference only - no FK. UserName is a snapshot so the log stays readable
            // after a user is renamed or removed, and an audit row must never fail to write.
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
            e.Property(t => t.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasOne(t => t.Device)
                .WithMany(d => d.RepairTickets)
                .HasForeignKey(t => t.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.AssignedTechnician)
                .WithMany()
                .HasForeignKey(t => t.AssignedTechnicianId)
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
                .WithMany(i => i.RepairTicketItems)
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Purchase>(e =>
        {
            e.Property(p => p.Notes).HasMaxLength(500);
            e.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.VoidReason).HasMaxLength(300);
            e.HasQueryFilter(p => !p.IsVoided);
            e.HasOne(p => p.Supplier)
                .WithMany(s => s.Purchases)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseItem>(e =>
        {
            e.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(i => !i.Purchase.IsVoided);
            e.HasOne(i => i.Purchase)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(i => i.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Item)
                .WithMany(i => i.PurchaseItems)
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesInvoice>(e =>
        {
            e.Property(s => s.InvoiceNumber).HasMaxLength(20);
            e.Property(s => s.CustomerName).HasMaxLength(100);
            e.Property(s => s.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(s => s.Total).HasColumnType("decimal(18,2)");
            e.Property(s => s.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Notes).HasMaxLength(500);
            e.Property(s => s.VoidReason).HasMaxLength(300);
            e.HasIndex(s => s.InvoiceNumber).IsUnique();
            // Voided rows are kept for audit but excluded from every normal query (reports, dashboards,
            // lists). Screens that show voided rows to the owner call IgnoreQueryFilters().
            e.HasQueryFilter(s => !s.IsVoided);
            e.HasOne(s => s.Customer)
                .WithMany(c => c.SalesInvoices)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CashSession>()
                .WithMany()
                .HasForeignKey(s => s.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesInvoiceLine>(e =>
        {
            e.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(l => l.UnitCost).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(l => !l.SalesInvoice.IsVoided);
            e.HasOne(l => l.SalesInvoice)
                .WithMany(s => s.Lines)
                .HasForeignKey(l => l.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Item)
                .WithMany(i => i.SalesInvoiceLines)
                .HasForeignKey(l => l.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerPayment>(e =>
        {
            e.Property(cp => cp.Amount).HasColumnType("decimal(18,2)");
            e.Property(cp => cp.Reference).HasMaxLength(100);
            e.Property(cp => cp.Notes).HasMaxLength(500);
            e.Property(cp => cp.VoidReason).HasMaxLength(300);
            e.HasQueryFilter(cp => !cp.IsVoided);
            e.HasOne(cp => cp.Customer)
                .WithMany(c => c.CustomerPayments)
                .HasForeignKey(cp => cp.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CashSession>()
                .WithMany()
                .HasForeignKey(cp => cp.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.Property(ex => ex.Category).HasMaxLength(100);
            e.Property(ex => ex.Description).HasMaxLength(500);
            e.Property(ex => ex.Amount).HasColumnType("decimal(18,2)");
            e.Property(ex => ex.Notes).HasMaxLength(500);
            e.Property(ex => ex.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(ex => ex.VoidReason).HasMaxLength(300);
            e.HasQueryFilter(ex => !ex.IsVoided);
        });

        modelBuilder.Entity<SupplierPayment>(e =>
        {
            e.Property(sp => sp.Amount).HasColumnType("decimal(18,2)");
            e.Property(sp => sp.Reference).HasMaxLength(100);
            e.Property(sp => sp.Notes).HasMaxLength(500);
            e.Property(sp => sp.VoidReason).HasMaxLength(300);
            e.HasQueryFilter(sp => !sp.IsVoided);
            e.HasOne(sp => sp.Supplier)
                .WithMany(s => s.SupplierPayments)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(sm => sm.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(sm => sm.Value).HasColumnType("decimal(18,2)");
            e.Property(sm => sm.Notes).HasMaxLength(500);
            e.HasOne(sm => sm.Item)
                .WithMany(i => i.StockMovements)
                .HasForeignKey(sm => sm.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashSession>(e =>
        {
            e.Property(c => c.OpenedByName).HasMaxLength(100);
            e.Property(c => c.ClosedByName).HasMaxLength(100);
            e.Property(c => c.OpeningBalance).HasColumnType("decimal(18,2)");
            e.Property(c => c.ExpectedAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.CountedAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.Difference).HasColumnType("decimal(18,2)");
            e.Property(c => c.Notes).HasMaxLength(500);
            e.HasIndex(c => c.BusinessDate);
        });

        modelBuilder.Entity<RepairTicketPayment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.CollectedByName).HasMaxLength(100);
            e.HasIndex(p => p.CashSessionId);
            e.HasOne(p => p.RepairTicket)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<CashSession>()
                .WithMany()
                .HasForeignKey(p => p.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>()
            .HasOne<CashSession>()
            .WithMany()
            .HasForeignKey(ex => ex.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalEntry>(e =>
        {
            e.Property(je => je.Description).HasMaxLength(500);
            e.Property(je => je.ReferenceType).HasMaxLength(50);
        });

        modelBuilder.Entity<JournalEntryLine>(e =>
        {
            e.Property(jl => jl.AccountName).HasMaxLength(100);
            e.Property(jl => jl.DebitAmount).HasColumnType("decimal(18,2)");
            e.Property(jl => jl.CreditAmount).HasColumnType("decimal(18,2)");
            e.HasOne(jl => jl.JournalEntry)
                .WithMany(je => je.Lines)
                .HasForeignKey(jl => jl.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
