using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Teaching.Queries.GetDaysOfWeekList;

public class GetDaysOfWeekListQuery : IRequest<Response<PaginatedResult<DayOfWeekDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
