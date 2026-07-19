using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.CancelGroupEnrollmentInvite;

public class CancelGroupEnrollmentInviteCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int EnrollmentRequestId { get; set; }
    public int StudentId { get; set; }
}
