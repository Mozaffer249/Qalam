using Microsoft.EntityFrameworkCore;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StudentEnrollmentQueryService : IStudentEnrollmentQueryService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public StudentEnrollmentQueryService(
        IEnrollmentRepository enrollmentRepository,
        IMediaUrlResolver mediaUrlResolver)
    {
        _enrollmentRepository = enrollmentRepository;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<(List<EnrollmentListItemDto> Items, int TotalCount)> ListForStudentAsync(
        int studentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _enrollmentRepository.GetByStudentIdQueryable(studentId);
        var totalCount = await query.CountAsync(cancellationToken);

        var enrollments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var items = enrollments.Select(e => MapListItem(e, utcNow)).ToList();
        return (items, totalCount);
    }

    private EnrollmentListItemDto MapListItem(Enrollment enrollment, DateTime utcNow)
    {
        var subject = enrollment.Course?.TeacherSubject?.Subject;
        var teacherUser = enrollment.ApprovedByTeacher?.User;
        var leaderUser = enrollment.LeaderStudent?.User;
        var schedules = enrollment.CourseSchedules ?? [];

        var dto = new EnrollmentListItemDto
        {
            Id = enrollment.Id,
            CourseId = enrollment.CourseId ?? 0,
            CourseTitle = enrollment.Course?.Title ?? "",
            CourseImageUrl = _mediaUrlResolver.ToPublicUrl(enrollment.Course?.ImageUrl),
            SubjectName = LocalizableEntity.GetLocalizedValue(subject?.NameAr, subject?.NameEn),
            Kind = enrollment.Kind,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            ApprovedAt = enrollment.ApprovedAt,
            TeacherDisplayName = teacherUser == null
                ? null
                : $"{teacherUser.FirstName} {teacherUser.LastName}".Trim(),
            TeacherImageUrl = _mediaUrlResolver.ToPublicUrl(teacherUser?.ProfilePictureUrl),
            ParticipantCount = enrollment.Participants?.Count ?? 0,
            LeaderStudentName = leaderUser == null
                ? null
                : $"{leaderUser.FirstName ?? ""} {leaderUser.LastName ?? ""}".Trim(),
            SessionsCount = enrollment.Course?.Sessions is { Count: > 0 } sessions
                ? sessions.Count
                : null,
            AmountDue = enrollment.AmountDue,
        };

        var completed = schedules.Count(s => s.Status == ScheduleStatus.Completed);
        dto.CompletedSessionsCount = completed;
        dto.ProgressPercent = dto.SessionsCount is int sessionsTotal && sessionsTotal > 0
            ? (int)Math.Round(completed * 100.0 / sessionsTotal)
            : null;
        dto.TeacherIsOnline = schedules.Any(s => s.TeacherInRoom);

        var next = EnrollmentScheduleHelper.ResolveNextSchedule(schedules, utcNow);
        if (next != null)
        {
            dto.NextScheduleId = next.Id;
            dto.NextSessionAt = EnrollmentScheduleHelper.ResolveScheduleStartUtc(next);
        }

        return dto;
    }
}
