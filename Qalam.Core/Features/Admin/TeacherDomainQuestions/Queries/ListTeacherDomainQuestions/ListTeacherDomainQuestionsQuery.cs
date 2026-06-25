using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.ListTeacherDomainQuestions;

public class ListTeacherDomainQuestionsQuery : IRequest<Response<List<TeacherDomainQuestionAdminDto>>>
{
    public int? DomainId { get; set; }
}
