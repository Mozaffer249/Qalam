using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.RevokeTeacherDomainApproval;

public class RevokeTeacherDomainApprovalCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int TeacherId { get; set; }
    public int DomainId { get; set; }
    public string Reason { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
