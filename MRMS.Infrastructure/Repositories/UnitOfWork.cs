using Microsoft.EntityFrameworkCore;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;
using MRMS.Infrastructure.Data;

namespace MRMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Repository<User>? _users;
    private Repository<Customer>? _customers;
    private Repository<Device>? _devices;
    private Repository<RepairTicket>? _repairTickets;
    private Repository<DevicePhoto>? _devicePhotos;
    private Repository<Warranty>? _warranties;
    private Repository<Item>? _items;
    private Repository<Supplier>? _suppliers;
    private Repository<SystemSetting>? _systemSettings;
    private Repository<RepairTicketItem>? _repairTicketItems;
    private Repository<SupplierPayment>? _supplierPayments;
    private Repository<StockMovement>? _stockMovements;
    private Repository<Purchase>? _purchases;
    private Repository<PurchaseItem>? _purchaseItems;
    private Repository<Sale>? _sales;
    private Repository<Expense>? _expenses;
    private Repository<JournalEntry>? _journalEntries;
    private Repository<JournalEntryLine>? _journalEntryLines;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Customer> Customers => _customers ??= new Repository<Customer>(_context);
    public IRepository<Device> Devices => _devices ??= new Repository<Device>(_context);
    public IRepository<RepairTicket> RepairTickets => _repairTickets ??= new Repository<RepairTicket>(_context);
    public IRepository<DevicePhoto> DevicePhotos => _devicePhotos ??= new Repository<DevicePhoto>(_context);
    public IRepository<Warranty> Warranties => _warranties ??= new Repository<Warranty>(_context);
    public IRepository<Item> Items => _items ??= new Repository<Item>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IRepository<SystemSetting> SystemSettings => _systemSettings ??= new Repository<SystemSetting>(_context);
    public IRepository<RepairTicketItem> RepairTicketItems => _repairTicketItems ??= new Repository<RepairTicketItem>(_context);
    public IRepository<SupplierPayment> SupplierPayments => _supplierPayments ??= new Repository<SupplierPayment>(_context);
    public IRepository<StockMovement> StockMovements => _stockMovements ??= new Repository<StockMovement>(_context);
    public IRepository<Purchase> Purchases => _purchases ??= new Repository<Purchase>(_context);
    public IRepository<PurchaseItem> PurchaseItems => _purchaseItems ??= new Repository<PurchaseItem>(_context);
    public IRepository<Sale> Sales => _sales ??= new Repository<Sale>(_context);
    public IRepository<Expense> Expenses => _expenses ??= new Repository<Expense>(_context);
    public IRepository<JournalEntry> JournalEntries => _journalEntries ??= new Repository<JournalEntry>(_context);
    public IRepository<JournalEntryLine> JournalEntryLines => _journalEntryLines ??= new Repository<JournalEntryLine>(_context);

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public async Task ExecuteInTransactionAsync(Func<Task> work)
    {
        // Already inside a transaction (nested call) - just run the work; the outermost caller commits.
        if (_context.Database.CurrentTransaction is not null)
        {
            await work();
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await work();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void Dispose() => _context.Dispose();
}
