using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int EnrollmentId { get; set; }
}
