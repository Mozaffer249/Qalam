using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class GuardianRepository : GenericRepositoryAsync<Guardian>, IGuardianRepository
{
    private readonly DbSet<Guardian> _guardians;

    public GuardianRepository(ApplicationDBContext context) : base(context)
    {
        _guardians = context.Set<Guardian>();
    }

    public async Task<Guardian?> GetByUserIdAsync(int userId)
    {
        return await _guardians
            .FirstOrDefaultAsync(g => g.UserId == userId);
    }
}
