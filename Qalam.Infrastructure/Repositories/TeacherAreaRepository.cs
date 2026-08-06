using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherAreaRepository : GenericRepositoryAsync<TeacherArea>, ITeacherAreaRepository
{
    private readonly ApplicationDBContext _context;
    private readonly DbSet<TeacherArea> _teacherAreas;
    private readonly DbSet<Location> _locations;

    public TeacherAreaRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
        _teacherAreas = context.Set<TeacherArea>();
        _locations = context.Set<Location>();
    }

    public async Task<List<TeacherArea>> GetByTeacherIdWithLocationAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _teacherAreas
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Include(a => a.Location)
                .ThenInclude(l => l.ParentLocation)
                    .ThenInclude(p => p!.ParentLocation)
                        .ThenInclude(gp => gp!.ParentLocation)
            .OrderBy(a => a.Location.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeacherArea> AddAsync(
        int teacherId,
        int locationId,
        decimal maxDistanceKm,
        CancellationToken cancellationToken = default)
    {
        var locationExists = await _locations.AnyAsync(l => l.Id == locationId && l.IsActive, cancellationToken);
        if (!locationExists)
            throw new InvalidOperationException("Location not found");

        var existing = await _teacherAreas
            .FirstOrDefaultAsync(a => a.TeacherId == teacherId && a.LocationId == locationId, cancellationToken);

        if (existing != null)
        {
            existing.MaxDistanceKm = maxDistanceKm;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (await GetOwnedWithLocationAsync(teacherId, existing.Id, cancellationToken))!;
        }

        var area = new TeacherArea
        {
            TeacherId = teacherId,
            LocationId = locationId,
            MaxDistanceKm = maxDistanceKm,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _teacherAreas.AddAsync(area, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetOwnedWithLocationAsync(teacherId, area.Id, cancellationToken))!;
    }

    public async Task<bool> DeleteOwnedAsync(
        int teacherId,
        int teacherAreaId,
        CancellationToken cancellationToken = default)
    {
        var area = await _teacherAreas
            .FirstOrDefaultAsync(a => a.Id == teacherAreaId && a.TeacherId == teacherId, cancellationToken);

        if (area == null)
            return false;

        _teacherAreas.Remove(area);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<TeacherArea?> GetOwnedWithLocationAsync(
        int teacherId,
        int teacherAreaId,
        CancellationToken cancellationToken)
    {
        return await _teacherAreas
            .AsNoTracking()
            .Where(a => a.Id == teacherAreaId && a.TeacherId == teacherId)
            .Include(a => a.Location)
                .ThenInclude(l => l.ParentLocation)
                    .ThenInclude(p => p!.ParentLocation)
                        .ThenInclude(gp => gp!.ParentLocation)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
