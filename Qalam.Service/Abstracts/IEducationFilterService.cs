using Qalam.Data.DTOs;

namespace Qalam.Service.Abstracts;

public interface IEducationFilterService
{
    Task<FilterOptionsResponseDto> GetFilterOptionsAsync(FilterStateDto state, int pageNumber = 1, int pageSize = 20);
}
