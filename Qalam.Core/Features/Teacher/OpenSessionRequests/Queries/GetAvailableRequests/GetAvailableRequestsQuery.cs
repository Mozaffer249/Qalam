using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequests;

public class GetAvailableRequestsQuery : IRequest<Response<PaginatedResult<TeacherAvailableRequestListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>Filter on the per-teacher target status. Null = all.</summary>
    public OpenSessionRequestTargetStatus? Status { get; set; }

    public int? SubjectId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public TeacherInboxSort SortBy { get; set; } = TeacherInboxSort.Newest;
}
