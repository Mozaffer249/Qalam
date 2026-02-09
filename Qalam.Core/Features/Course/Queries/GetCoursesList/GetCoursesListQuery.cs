using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Course.Queries.GetCoursesList;

public class GetCoursesListQuery : IRequest<Response<PaginatedResult<CourseListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? DomainId { get; set; }
    public CourseStatus? Status { get; set; }
    public int? SubjectId { get; set; }
}
