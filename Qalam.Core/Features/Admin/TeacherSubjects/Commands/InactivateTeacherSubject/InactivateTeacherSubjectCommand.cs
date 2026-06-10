using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.InactivateTeacherSubject;

public class InactivateTeacherSubjectCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int TeacherId { get; set; }
    public int TeacherSubjectId { get; set; }
}
