using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.CreateTeacherDomainQuestion;

public class CreateTeacherDomainQuestionCommand : IRequest<Response<TeacherDomainQuestionAdminDto>>
{
    public CreateTeacherDomainQuestionDto Data { get; set; } = null!;
}
