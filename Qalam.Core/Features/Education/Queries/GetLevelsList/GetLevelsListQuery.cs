using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Education.Queries.GetLevelsList;

public class GetLevelsListQuery : IRequest<Response<PaginatedResult<EducationLevel>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public string? Search { get; set; }
}
