using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQuery : IRequest<Response<PaginatedResult<EnrollmentListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
