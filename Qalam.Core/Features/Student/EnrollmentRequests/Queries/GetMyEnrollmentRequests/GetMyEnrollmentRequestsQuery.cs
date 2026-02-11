using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;

public class GetMyEnrollmentRequestsQuery : IRequest<Response<PaginatedResult<EnrollmentRequestListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public RequestStatus? Status { get; set; }
}
