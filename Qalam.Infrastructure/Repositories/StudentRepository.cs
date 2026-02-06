using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class StudentRepository : GenericRepositoryAsync<Student>, IStudentRepository
{
    private readonly DbSet<Student> _students;

    public StudentRepository(ApplicationDBContext context) : base(context)
    {
        _students = context.Set<Student>();
    }

    public async Task<Student?> GetByUserIdAsync(int userId)
    {
        return await _students
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }
}
