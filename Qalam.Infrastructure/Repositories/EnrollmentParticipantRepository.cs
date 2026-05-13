using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EnrollmentParticipantRepository : GenericRepositoryAsync<EnrollmentParticipant>, IEnrollmentParticipantRepository
{
    private readonly ApplicationDBContext _context;

    public EnrollmentParticipantRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<EnrollmentParticipant?> GetByIdForPaymentAsync(int id, CancellationToken ct)
    {
        return await _context.EnrollmentParticipants
            .Include(p => p.Student).ThenInclude(s => s.User)
            .Include(p => p.Student).ThenInclude(s => s.Guardian)
            .Include(p => p.Enrollment).ThenInclude(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(p => p.Enrollment).ThenInclude(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(p => p.Enrollment).ThenInclude(e => e.Course).ThenInclude(c => c.Sessions)
            .Include(p => p.Enrollment).ThenInclude(e => e.Participants)
            .Include(p => p.Enrollment).ThenInclude(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(p => p.Enrollment).ThenInclude(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(p => p.Enrollment).ThenInclude(e => e.EnrollmentRequest!).ThenInclude(r => r.ProposedSessions)
            .Include(p => p.Enrollment).ThenInclude(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(p => p.Enrollment).ThenInclude(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
