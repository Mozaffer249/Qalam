using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.GetTeacherDomainQuestionById;

public class GetTeacherDomainQuestionByIdQuery : IRequest<Response<TeacherDomainQuestionAdminDto>>
{
    public int Id { get; set; }
}
