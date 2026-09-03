using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;

    public SystemSettingService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task<IEnumerable<SystemSettingDto>> GetAllAsync()
    {
        var settings = await _uow.SystemSettings.GetAllAsync();
        return settings.Select(s => new SystemSettingDto(s.Id, s.Key, s.Value));
    }

    public async Task<SystemSettingDto?> GetByKeyAsync(string key)
    {
        var settings = await _uow.SystemSettings.FindAsync(s => s.Key == key);
        var setting = settings.FirstOrDefault();
        return setting is null ? null : new SystemSettingDto(setting.Id, setting.Key, setting.Value);
    }

    public async Task<SystemSettingDto> UpdateAsync(UpdateSystemSettingDto dto)
    {
        _permissions.Require(Permission.ManageSettings);
        var settings = await _uow.SystemSettings.FindAsync(s => s.Id == dto.Id);
        var setting = settings.FirstOrDefault();
        if (setting is null)
            throw new KeyNotFoundException($"SystemSetting {dto.Id} not found");

        setting.Value = dto.Value;
        _uow.SystemSettings.Update(setting);
        await _uow.CompleteAsync();
        return new SystemSettingDto(setting.Id, setting.Key, setting.Value);
    }

    public async Task<string[]> GetPaymentMethodsAsync()
    {
        var setting = await GetByKeyAsync("PaymentMethods");
        return setting is null ? [] : setting.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task<string[]> GetExpenseCategoriesAsync()
    {
        var setting = await GetByKeyAsync("ExpenseCategories");
        return setting is null ? [] : setting.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
