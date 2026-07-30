namespace MRMS.Application.DTOs;

public record SystemSettingDto(int Id, string Key, string Value);

public record UpdateSystemSettingDto(int Id, string Value);
