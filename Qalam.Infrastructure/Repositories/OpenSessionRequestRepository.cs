using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class OpenSessionRequestRepository : GenericRepositoryAsync<OpenSessionRequest>, IOpenSessionRequestRepository
{
    private readonly ApplicationDBContext _context;

    public OpenSessionRequestRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<int?> GetSubjectIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => (int?)r.SubjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeacherAvailableRequestDetailDto?> GetTeacherDetailDtoAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new TeacherAvailableRequestDetailDto
            {
                Id = r.Id,
                Status = r.Status,
                Content = new RequestContentDto
                {
                    DomainId = r.DomainId,
                    DomainNameEn = r.Domain != null ? r.Domain.NameEn : null,
                    DomainNameAr = r.Domain != null ? r.Domain.NameAr : null,
                    CurriculumId = r.CurriculumId,
                    CurriculumNameEn = r.Curriculum != null ? r.Curriculum.NameEn : null,
                    LevelId = r.LevelId,
                    LevelNameEn = r.Level != null ? r.Level.NameEn : null,
                    GradeId = r.GradeId,
                    GradeNameEn = r.Grade != null ? r.Grade.NameEn : null,
                    SubjectId = r.SubjectId,
                    SubjectNameEn = r.Subject != null ? r.Subject.NameEn : null,
                    SubjectNameAr = r.Subject != null ? r.Subject.NameAr : null,
                },
                GeneralSettings = new RequestGeneralSettingsDto
                {
                    SessionsCount = r.TotalSessionsCount,
                    DefaultDurationMinutes = r.Sessions.Select(s => (int?)s.DurationMinutes).FirstOrDefault(),
                    TeachingModeId = r.TeachingModeId,
                    TeachingModeNameEn = r.TeachingMode != null ? r.TeachingMode.NameEn : null,
                    GroupType = r.GroupType,
                    StudentNotes = r.StudentNotes,
                },
                Sessions = r.Sessions
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => new TeacherViewSessionDto
                    {
                        Id = s.Id,
                        SequenceNumber = s.SequenceNumber,
                        PreferredDate = s.PreferredDate,
                        TimeSlotId = s.TimeSlotId,
                        TimeSlotLabelEn = s.TimeSlot != null ? s.TimeSlot.LabelEn : null,
                        DurationMinutes = s.DurationMinutes,
                        Notes = s.Notes,
                        Units = s.Units.Select(u => new TeacherViewSessionUnitDto
                        {
                            Id = u.Id,
                            ContentUnitId = u.ContentUnitId,
                            ContentUnitNameEn = u.ContentUnit != null ? u.ContentUnit.NameEn : null,
                            ContentUnitNameAr = u.ContentUnit != null ? u.ContentUnit.NameAr : null,
                            LessonId = u.LessonId,
                            LessonNameEn = u.Lesson != null ? u.Lesson.NameEn : null,
                            LessonNameAr = u.Lesson != null ? u.Lesson.NameAr : null,
                        }).ToList()
                    }).ToList(),
                Student = new RequestStudentSummaryDto
                {
                    Id = r.StudentId,
                    DisplayName = r.Student != null && r.Student.User != null
                        ? (r.Student.User.FirstName ?? "") + " " + (r.Student.User.LastName ?? "")
                        : null,
                },
                CurrentOffersCount = r.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn),
                MyOfferStatus = null,
                MyOfferId = null,
                ExpiresAt = r.ExpiresAt ?? DateTime.MinValue,
                PublishedAt = r.PublishedAt ?? DateTime.MinValue,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<RequestSessionScheduleSlot>> GetSessionScheduleSlotsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequestSessions
            .AsNoTracking()
            .Where(s => s.SessionRequestId == requestId)
            .OrderBy(s => s.SequenceNumber)
            .Select(s => new RequestSessionScheduleSlot(
                s.Id,
                s.SequenceNumber,
                s.PreferredDate,
                s.TimeSlotId,
                s.DurationMinutes,
                s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.StartTime : null,
                s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.EndTime : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveOffersAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionOffers
            .AsNoTracking()
            .CountAsync(o => o.SessionRequestId == requestId
                             && o.Status != OpenSessionOfferStatus.Withdrawn, cancellationToken);
    }

    public async Task<RequestStatusSummary?> GetStatusSummaryAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new RequestStatusSummary(
                r.Id,
                r.StudentId,
                r.RequestedByUserId,
                r.CreatedByGuardianId,
                r.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(int requestId, OpenSessionRequestStatus newStatus, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OpenSessionRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null) return false;

        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
