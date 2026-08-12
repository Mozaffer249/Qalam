using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitationById;

public class GetMyInvitationByIdQuery : IRequest<Response<StudentInvitationDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// <c>EnrollmentRequest-{id}</c> or <c>OpenSessionRequest-{id}</c>.
    /// </summary>
    public string InvitationKey { get; set; } = string.Empty;
}
