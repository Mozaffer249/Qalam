using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherAreaRepository : IGenericRepositoryAsync<TeacherArea>
{
    Task<List<TeacherArea>> GetByTeacherIdWithLocationAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<TeacherArea> AddAsync(int teacherId, int locationId, decimal maxDistanceKm, CancellationToken cancellationToken = default);

    Task<bool> DeleteOwnedAsync(int teacherId, int teacherAreaId, CancellationToken cancellationToken = default);
}
