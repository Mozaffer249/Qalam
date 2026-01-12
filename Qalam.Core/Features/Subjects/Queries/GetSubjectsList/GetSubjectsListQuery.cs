using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectsList;

public class GetSubjectsListQuery : IRequest<Response<PaginatedResult<Subject>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public string? Search { get; set; }
}
