using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class SessionTypeRepository : GenericRepositoryAsync<SessionType>, ISessionTypeRepository
{
    private readonly ApplicationDBContext _context;

    public SessionTypeRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<SessionType> GetSessionTypesQueryable()
    {
        return _context.SessionTypes
            .AsNoTracking()
            .OrderBy(st => st.NameEn);
    }

    public IQueryable<SessionType> GetActiveSessionTypesQueryable()
    {
        // SessionType doesn't have IsActive property
        return _context.SessionTypes
            .AsNoTracking()
            .OrderBy(st => st.NameEn);
    }

    public async Task<SessionType> GetSessionTypeByCodeAsync(string code)
    {
        return await _context.SessionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Code == code);
    }

    public async Task<bool> IsSessionTypeCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.SessionTypes.Where(st => st.Code == code);
        if (excludeId.HasValue)
            query = query.Where(st => st.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}
