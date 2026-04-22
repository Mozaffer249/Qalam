using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Teaching.Queries.GetDaysOfWeekList;

public class GetDaysOfWeekListQuery : IRequest<Response<List<DayOfWeekDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
