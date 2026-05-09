using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;

public class GetCourseEnrollmentsListQuery : IRequest<Response<List<TeacherEnrollmentListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int CourseId { get; set; }

    /// <summary>Optional. Filter by status (PendingPayment, Active, Cancelled, Completed).</summary>
    public EnrollmentStatus? Status { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
