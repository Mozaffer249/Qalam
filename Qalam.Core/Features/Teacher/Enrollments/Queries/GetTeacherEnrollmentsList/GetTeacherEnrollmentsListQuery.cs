using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentsList;

public class GetTeacherEnrollmentsListQuery : IRequest<Response<List<TeacherEnrollmentListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>Optional. Filter by status (PendingPayment, Active, Cancelled, Completed).</summary>
    public EnrollmentStatus? Status { get; set; }

    /// <summary>Optional. CourseRequest or SessionRequest.</summary>
    public EnrollmentSource? Source { get; set; }

    /// <summary>Optional. Individual or Group.</summary>
    public EnrollmentKind? Kind { get; set; }

    /// <summary>Optional. Matches student/group display name or course title.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Optional badge filter: FixedCourse, FlexibleCourse, PublishedRequest, DirectedRequest.
    /// Applied in-memory after load (depends on course / open-request flags).
    /// </summary>
    public TeacherEnrollmentSourceBadge? SourceBadge { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
