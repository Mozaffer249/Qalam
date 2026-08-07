using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;

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

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
