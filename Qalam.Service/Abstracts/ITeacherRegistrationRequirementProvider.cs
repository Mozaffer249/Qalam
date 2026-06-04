using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationRequirementProvider
{
    Task<List<TeacherRegistrationRequirement>> GetActiveRequirementsAsync(CancellationToken cancellationToken = default);
    Task<List<TeacherRegistrationRequirement>> GetAllRequirementsAsync(CancellationToken cancellationToken = default);
    Task<List<TeacherRegistrationRequirementPublicDto>> GetActivePublicDtosAsync(CancellationToken cancellationToken = default);
    TeacherRegistrationRequirementPublicDto ToPublicDto(TeacherRegistrationRequirement entity);
    TeacherRegistrationRequirementAdminDto ToAdminDto(TeacherRegistrationRequirement entity);
}
