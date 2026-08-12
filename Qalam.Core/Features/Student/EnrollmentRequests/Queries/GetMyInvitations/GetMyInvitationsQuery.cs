using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitations;

public class GetMyInvitationsQuery : IRequest<Response<List<StudentInvitationListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Active (pending) or Archived (history). Default Active.</summary>
    public InvitationInboxScope Scope { get; set; } = InvitationInboxScope.Active;
}
