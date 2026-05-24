using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.RespondToOpenSessionRequestInvitation;

public class RespondToOpenSessionRequestInvitationCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int OpenSessionRequestId { get; set; }

    public RespondToOpenSessionRequestInvitationDto Data { get; set; } = null!;
}
