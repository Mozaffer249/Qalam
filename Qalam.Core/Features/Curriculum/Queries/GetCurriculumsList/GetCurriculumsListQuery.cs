using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Curriculum.Queries.GetCurriculumsList;

public class GetCurriculumsListQuery : IRequest<Response<PaginatedResult<CurriculumDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? DomainId { get; set; }
}
