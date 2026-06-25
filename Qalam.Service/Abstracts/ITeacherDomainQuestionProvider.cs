using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherDomainQuestionProvider
{
    TeacherDomainQuestionPublicDto ToPublicDto(TeacherDomainQuestion entity, TeacherDomainQuestionSubmission? submission = null);
    TeacherDomainQuestionAdminDto ToAdminDto(TeacherDomainQuestion entity);
}
