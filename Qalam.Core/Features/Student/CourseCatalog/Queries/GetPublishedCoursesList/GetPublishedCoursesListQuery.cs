using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;

public class GetPublishedCoursesListQuery : IRequest<Response<PaginatedResult<CourseCatalogItemDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? DomainId { get; set; }
    public int? SubjectId { get; set; }
    public int? TeachingModeId { get; set; }
}
