using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class ItemService : IItemService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;

    public ItemService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task<IEnumerable<ItemDto>> GetAllAsync()
    {
        var items = await _uow.Items.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<ItemDto?> GetByIdAsync(int id)
    {
        var item = await _uow.Items.GetByIdAsync(id);
        return item is null ? null : MapToDto(item);
    }

    public async Task<ItemDto?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var items = await _uow.Items.FindAsync(i => i.Code == code);
        var item = items.FirstOrDefault();
        return item is null ? null : MapToDto(item);
    }

    public async Task<ItemDto> CreateAsync(CreateItemDto dto)
    {
        var item = new Item
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            Unit = dto.Unit,
            CostPrice = dto.CostPrice,
            SellingPrice = dto.SellingPrice,
            ReorderLevel = dto.ReorderLevel,
            CurrentStock = 0,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Items.AddAsync(item);
        await _uow.CompleteAsync();
        return MapToDto(item);
    }

    public async Task<ItemDto> UpdateAsync(UpdateItemDto dto)
    {
        var item = await _uow.Items.GetByIdAsync(dto.Id);
        if (item is null)
            throw new KeyNotFoundException($"Item {dto.Id} not found");

        item.Code = dto.Code;
        item.Name = dto.Name;
        item.Type = dto.Type;
        item.Unit = dto.Unit;
        item.CostPrice = dto.CostPrice;
        item.SellingPrice = dto.SellingPrice;
        item.ReorderLevel = dto.ReorderLevel;
        _uow.Items.Update(item);
        await _uow.CompleteAsync();
        return MapToDto(item);
    }

    public async Task UpdatePricesAsync(int id, decimal costPrice, decimal sellingPrice)
    {
        var item = await _uow.Items.GetByIdAsync(id);
        if (item is null)
            throw new KeyNotFoundException($"Item {id} not found");

        item.CostPrice = costPrice;
        item.SellingPrice = sellingPrice;
        _uow.Items.Update(item);
        await _uow.CompleteAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _permissions.Require(Permission.DeleteEntities);
        var item = await _uow.Items.GetByIdAsync(id);
        if (item is null) return false;

        _uow.Items.Delete(item);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<IEnumerable<ItemDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<ItemDto>();

        var items = await _uow.Items.FindAsync(
            i => i.Name.Contains(query) || i.Code.Contains(query));
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<ItemDto>> GetLowStockAsync()
    {
        var items = await _uow.Items.FindAsync(
            i => i.CurrentStock <= i.ReorderLevel);
        return items.Select(MapToDto);
    }

    private ItemDto MapToDto(Item i) =>
        new()
        {
            Id = i.Id,
            Code = i.Code,
            Name = i.Name,
            Type = i.Type,
            Unit = i.Unit,
            CostPrice = _permissions.Can(Permission.ViewCostPrice) ? i.CostPrice : null,
            SellingPrice = i.SellingPrice,
            ReorderLevel = i.ReorderLevel,
            CurrentStock = i.CurrentStock,
            CreatedAt = i.CreatedAt
        };
}
