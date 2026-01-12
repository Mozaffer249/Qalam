using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Content.Queries.GetLessonsList;

public class GetLessonsListQuery : IRequest<Response<PaginatedResult<Lesson>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? ContentUnitId { get; set; }
    public int? SubjectId { get; set; }
    public string? Search { get; set; }
}
