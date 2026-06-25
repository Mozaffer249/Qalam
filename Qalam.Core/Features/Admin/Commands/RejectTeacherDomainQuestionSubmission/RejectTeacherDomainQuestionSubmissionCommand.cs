using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.RejectTeacherDomainQuestionSubmission;

public class RejectTeacherDomainQuestionSubmissionCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int SubmissionId { get; set; }
    public string Reason { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
