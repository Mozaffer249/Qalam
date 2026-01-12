using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Content.Queries.GetContentUnitsList;

public class GetContentUnitsListQuery : IRequest<Response<PaginatedResult<ContentUnit>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? SubjectId { get; set; }
    public int? TermId { get; set; }
    public string? Search { get; set; }
}
