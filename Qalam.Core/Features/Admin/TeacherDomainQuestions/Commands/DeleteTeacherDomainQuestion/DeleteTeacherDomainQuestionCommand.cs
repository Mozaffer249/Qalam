using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.DeleteTeacherDomainQuestion;

public class DeleteTeacherDomainQuestionCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
}
