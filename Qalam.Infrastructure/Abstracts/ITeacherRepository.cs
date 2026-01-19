using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherRepository : IGenericRepositoryAsync<Teacher>
{
    Task<Teacher?> GetByUserIdAsync(int userId);
    Task UpdateStatusAsync(int teacherId, TeacherStatus status);
    Task UpdateLocationAsync(int teacherId, TeacherLocation location);
}
