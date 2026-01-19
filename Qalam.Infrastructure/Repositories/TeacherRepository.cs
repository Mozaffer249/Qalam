using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherRepository : GenericRepositoryAsync<Teacher>, ITeacherRepository
{
    private readonly DbSet<Teacher> _teachers;

    public TeacherRepository(ApplicationDBContext context) : base(context)
    {
        _teachers = context.Set<Teacher>();
    }

    public async Task<Teacher?> GetByUserIdAsync(int userId)
    {
        return await _teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);
    }

    public async Task UpdateStatusAsync(int teacherId, TeacherStatus status)
    {
        var teacher = await _teachers.FindAsync(teacherId);
        if (teacher != null)
        {
            teacher.Status = status;
            _teachers.Update(teacher);
        }
    }

    public async Task UpdateLocationAsync(int teacherId, TeacherLocation location)
    {
        var teacher = await _teachers.FindAsync(teacherId);
        if (teacher != null)
        {
            teacher.Location = location;
            _teachers.Update(teacher);
        }
    }
}
