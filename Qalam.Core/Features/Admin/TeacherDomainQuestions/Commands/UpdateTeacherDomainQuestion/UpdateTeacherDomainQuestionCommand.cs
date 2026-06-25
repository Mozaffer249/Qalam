using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.UpdateTeacherDomainQuestion;

public class UpdateTeacherDomainQuestionCommand : IRequest<Response<TeacherDomainQuestionAdminDto>>
{
    public int Id { get; set; }
    public UpdateTeacherDomainQuestionDto Data { get; set; } = null!;
}
