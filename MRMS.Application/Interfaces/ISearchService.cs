using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SearchResultDto>> SearchAsync(string query);
}
