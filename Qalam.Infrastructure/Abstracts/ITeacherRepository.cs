using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherRepository : IGenericRepositoryAsync<Teacher>
{
    Task<Teacher?> GetByUserIdAsync(int userId);
    Task UpdateStatusAsync(int teacherId, TeacherStatus status);
    Task UpdateLocationAsync(int teacherId, TeacherLocation location);
    
    // Admin operations
    IQueryable<Teacher> GetPendingTeachersQueryable();
    Task<int> CountAsync(IQueryable<Teacher> query);
    Task<List<PendingTeacherDto>> GetPendingTeachersDtoAsync(int pageNumber, int pageSize);
    Task<TeacherDetailsDto?> GetTeacherDetailsAsync(int teacherId);
}
