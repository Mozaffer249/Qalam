using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IGuardianRepository : IGenericRepositoryAsync<Guardian>
{
    Task<Guardian?> GetByUserIdAsync(int userId);
}
