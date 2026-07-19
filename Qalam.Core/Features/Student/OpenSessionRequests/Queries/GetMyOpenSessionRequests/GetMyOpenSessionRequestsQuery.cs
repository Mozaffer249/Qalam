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

    /// <summary>Optional status filter.</summary>
    public OpenSessionRequestStatus? Status { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
