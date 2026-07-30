using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISystemSettingService
{
    Task<IEnumerable<SystemSettingDto>> GetAllAsync();
    Task<SystemSettingDto?> GetByKeyAsync(string key);
    Task<SystemSettingDto> UpdateAsync(UpdateSystemSettingDto dto);
    Task<string[]> GetPaymentMethodsAsync();
    Task<string[]> GetExpenseCategoriesAsync();
}
