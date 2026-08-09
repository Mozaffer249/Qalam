using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveTeacherDomain;

public class ApproveTeacherDomainCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int TeacherId { get; set; }
    public int DomainId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
