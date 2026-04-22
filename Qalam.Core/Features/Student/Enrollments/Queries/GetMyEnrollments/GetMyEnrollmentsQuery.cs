using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQuery : IRequest<Response<List<EnrollmentListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
