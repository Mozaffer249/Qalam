using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Services;

public class OpenSessionRequestAccessGuard : IOpenSessionRequestAccessGuard
{
    private readonly ApplicationDBContext _db;

    public OpenSessionRequestAccessGuard(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<AccessGuardResult> CanCreateForStudentAsync(int currentUserId, int targetStudentId, CancellationToken ct)
    {
        var student = await _db.Students
            .Where(s => s.Id == targetStudentId)
            .Select(s => new { s.Id, s.UserId, s.IsMinor, s.GuardianId })
            .FirstOrDefaultAsync(ct);

        if (student is null)
            return AccessGuardResult.Deny("الطالب غير موجود");

        // Path 1: student is acting for themselves (only if they're an adult)
        if (!student.IsMinor && student.UserId == currentUserId)
            return AccessGuardResult.Allow();

        // Path 2: a guardian is acting for their child
        if (student.GuardianId is int gid)
        {
            var guardian = await _db.Guardians
                .Where(g => g.Id == gid && g.IsActive)
                .Select(g => new { g.Id, g.UserId })
                .FirstOrDefaultAsync(ct);

            if (guardian?.UserId == currentUserId)
                return AccessGuardResult.AllowAsGuardian(guardian.Id);
        }

        return AccessGuardResult.Deny("غير مصرح لك بإنشاء طلب لهذا الطالب");
    }

    public async Task<bool> CanActOnRequestAsync(int currentUserId, OpenSessionRequest request, CancellationToken ct)
    {
        if (request.RequestedByUserId == currentUserId)
            return true;

        if (request.CreatedByGuardianId is int gid)
        {
            return await _db.Guardians
                .AnyAsync(g => g.Id == gid && g.UserId == currentUserId && g.IsActive, ct);
        }

        return false;
    }

    public async Task<bool> CanRespondToInvitationAsync(int currentUserId, int invitedStudentId, CancellationToken ct)
    {
        var student = await _db.Students
            .Where(s => s.Id == invitedStudentId)
            .Select(s => new { s.UserId, s.IsMinor, s.GuardianId })
            .FirstOrDefaultAsync(ct);

        if (student is null) return false;

        if (!student.IsMinor && student.UserId == currentUserId) return true;

        if (student.GuardianId is int gid)
        {
            return await _db.Guardians
                .AnyAsync(g => g.Id == gid && g.UserId == currentUserId && g.IsActive, ct);
        }

        return false;
    }
}
