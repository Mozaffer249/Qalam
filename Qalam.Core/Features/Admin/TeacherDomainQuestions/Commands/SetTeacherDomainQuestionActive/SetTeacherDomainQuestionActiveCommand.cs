using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.SetTeacherDomainQuestionActive;

public class SetTeacherDomainQuestionActiveCommand : IRequest<Response<TeacherDomainQuestionAdminDto>>
{
    public int Id { get; set; }
    public SetRequirementActiveDto Data { get; set; } = null!;
}
