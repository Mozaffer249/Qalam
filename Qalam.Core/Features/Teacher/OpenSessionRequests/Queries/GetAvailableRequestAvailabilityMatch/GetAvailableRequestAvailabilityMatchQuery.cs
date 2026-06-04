using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestAvailabilityMatch;

public class GetAvailableRequestAvailabilityMatchQuery
    : IRequest<Response<List<SessionAvailabilityMatchDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int RequestId { get; set; }
}
