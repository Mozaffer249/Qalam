using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;

public class RespondToGroupEnrollmentInviteCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int EnrollmentRequestId { get; set; }
    public RespondToGroupEnrollmentInviteDto Data { get; set; } = null!;
}
