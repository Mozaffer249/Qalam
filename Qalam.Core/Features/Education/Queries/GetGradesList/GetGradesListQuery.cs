using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Education.Queries.GetGradesList;

public class GetGradesListQuery : IRequest<Response<PaginatedResult<GradeDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? LevelId { get; set; }
     public string? Search { get; set; }
}
