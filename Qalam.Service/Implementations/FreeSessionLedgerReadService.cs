using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class FreeSessionLedgerReadService : IFreeSessionLedgerReadService
{
    private readonly ApplicationDBContext _db;

    public FreeSessionLedgerReadService(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<List<AdminStudentFreeTrialConsumptionDto>> ListStudentConsumptionsAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _db.StudentFreeTrialConsumptions
            .AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.ReservedAt)
            .Select(c => new AdminStudentFreeTrialConsumptionDto
            {
                Id = c.Id,
                StudentId = c.StudentId,
                Source = c.Source.ToString(),
                EnrollmentId = c.EnrollmentId,
                OpenSessionRequestId = c.OpenSessionRequestId,
                TeacherId = c.TeacherId,
                DomainId = c.DomainId,
                CourseScheduleId = c.CourseScheduleId,
                Status = c.Status.ToString(),
                ReservedAt = c.ReservedAt,
                ConsumedAt = c.ConsumedAt,
                CancelledAt = c.CancelledAt,
                RestoredEligibility = c.RestoredEligibility,
                CancelReason = c.CancelReason,
                CancelledByUserId = c.CancelledByUserId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AdminTeacherInterviewUnlockDto>> ListTeacherInterviewUnlocksAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _db.TeacherDomainPricings
            .AsNoTracking()
            .Where(p => p.TeacherId == teacherId)
            .OrderBy(p => p.DomainId)
            .Select(p => new AdminTeacherInterviewUnlockDto
            {
                DomainId = p.DomainId,
                DomainNameEn = p.Domain.NameEn,
                DomainNameAr = p.Domain.NameAr,
                HasCompletedInterviewSession = p.HasCompletedInterviewSession,
                InterviewUnlockSource = p.InterviewUnlockSource.ToString(),
                InterviewUnlockEnrollmentId = p.InterviewUnlockEnrollmentId,
                InterviewUnlockCourseScheduleId = p.InterviewUnlockCourseScheduleId,
                InterviewUnlockedAt = p.InterviewUnlockedAt,
                InterviewRevertedAt = p.InterviewRevertedAt,
                TeacherLevelId = p.TeacherLevelId
            })
            .ToListAsync(cancellationToken);
    }
}
