using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetMyOpenSessionRequests;

public class GetMyOpenSessionRequestsQuery
    : IRequest<Response<List<OpenSessionRequestListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>Optional exact status filter. When set, wins over Scope.</summary>
    public OpenSessionRequestStatus? Status { get; set; }

    /// <summary>Active (default) = still open for the student; Archived = terminal; All = no scope filter.</summary>
    public OpenSessionRequestScope Scope { get; set; } = OpenSessionRequestScope.Active;

    /// <summary>When true, only targeted requests; when false, only broadcast.</summary>
    public bool? IsTargeted { get; set; }

    /// <summary>Learner filter (guardian picking one child).</summary>
    public int? StudentId { get; set; }

    public TeacherInboxSort SortBy { get; set; } = TeacherInboxSort.Newest;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
