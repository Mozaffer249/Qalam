using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IStudentRepository : IGenericRepositoryAsync<Student>
{
    Task<Student?> GetByUserIdAsync(int userId);
}
