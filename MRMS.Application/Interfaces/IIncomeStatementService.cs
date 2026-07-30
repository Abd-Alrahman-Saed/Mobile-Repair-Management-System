using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IIncomeStatementService
{
    Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to);
}
