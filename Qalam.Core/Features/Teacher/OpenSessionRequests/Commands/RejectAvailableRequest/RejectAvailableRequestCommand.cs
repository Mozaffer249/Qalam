using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.RejectAvailableRequest;

public class RejectAvailableRequestCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int RequestId { get; set; }
}
