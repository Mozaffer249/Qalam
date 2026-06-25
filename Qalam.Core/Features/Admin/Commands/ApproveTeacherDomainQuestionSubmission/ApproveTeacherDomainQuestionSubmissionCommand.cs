using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveTeacherDomainQuestionSubmission;

public class ApproveTeacherDomainQuestionSubmissionCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int SubmissionId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
