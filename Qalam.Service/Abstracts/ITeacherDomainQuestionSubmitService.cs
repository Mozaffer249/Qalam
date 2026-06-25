using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherDomainQuestionSubmitService
{
    Task SubmitAsync(
        Teacher teacher,
        TeacherDomainQuestionSubmissionInput input,
        List<TeacherDomainQuestion> activeQuestions,
        CancellationToken cancellationToken);
}
