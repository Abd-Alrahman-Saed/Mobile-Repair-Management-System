namespace MRMS.Application.DTOs;

public record CustomerDto(
    int Id,
    string FullName,
    string PhoneNumber,
    DateTime CreatedAt);

public record CreateCustomerDto(string FullName, string PhoneNumber);

public record UpdateCustomerDto(int Id, string FullName, string PhoneNumber);
