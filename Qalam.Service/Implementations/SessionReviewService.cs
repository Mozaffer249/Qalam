using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SessionReviewService : ISessionReviewService
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ApplicationDBContext _context;
    private readonly ILogger<SessionReviewService> _logger;

    public SessionReviewService(
        IStudentRepository studentRepository,
        ICourseScheduleRepository scheduleRepository,
        ITeacherRepository teacherRepository,
        ApplicationDBContext context,
        ILogger<SessionReviewService> logger)
    {
        _studentRepository = studentRepository;
        _scheduleRepository = scheduleRepository;
        _teacherRepository = teacherRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> SubmitStudentReviewAsync(
        int userId,
        int courseScheduleId,
        int rating,
        string? feedback,
        CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
            return (false, "Rating must be between 1 and 5.", false, false);

        var trimmed = feedback?.Trim();
        if (trimmed is { Length: > 600 })
            return (false, "Feedback is too long.", false, false);

        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            return (false, "Student profile not found.", false, true);

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(courseScheduleId, cancellationToken);
        if (schedule == null)
            return (false, "Session not found.", false, true);

        if (schedule.Status != ScheduleStatus.Completed)
            return (false, "Reviews are only available after the session is completed.", false, false);

        if (!schedule.Enrollment.Participants.Any(p => p.StudentId == student.Id))
            return (false, "You are not a participant in this enrollment.", true, false);

        var teacherId = schedule.Enrollment.Course?.TeacherId
                        ?? (schedule.Enrollment.ApprovedByTeacherId > 0
                            ? schedule.Enrollment.ApprovedByTeacherId
                            : (int?)null);
        if (teacherId is null or <= 0)
            return (false, "Teacher for this session could not be resolved.", false, false);

        var exists = await _context.TeacherReviews
            .AnyAsync(r => r.StudentId == student.Id && r.SessionId == courseScheduleId, cancellationToken);
        if (exists)
            return (false, "You have already reviewed this session.", false, false);

        _context.TeacherReviews.Add(new TeacherReview
        {
            TeacherId = teacherId.Value,
            StudentId = student.Id,
            SessionId = courseScheduleId,
            Rating = rating,
            Feedback = trimmed,
            IsApproved = true,
        });

        await _context.SaveChangesAsync(cancellationToken);
        await RecalculateTeacherRatingAverageAsync(teacherId.Value, cancellationToken);

        _logger.LogInformation(
            "Student {StudentId} reviewed CourseSchedule {ScheduleId} for teacher {TeacherId}.",
            student.Id, courseScheduleId, teacherId.Value);

        return (true, "Review submitted.", false, false);
    }

    public async Task<List<SessionReviewDto>> GetReviewsForSessionAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var studentToTeacher = await _context.TeacherReviews
            .AsNoTracking()
            .Where(r => r.SessionId == courseScheduleId && r.IsApproved)
            .Select(r => new SessionReviewDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentName = r.Student != null && r.Student.User != null
                    ? ((r.Student.User.FirstName ?? "") + " " + (r.Student.User.LastName ?? "")).Trim()
                    : $"Student #{r.StudentId}",
                Rating = r.Rating,
                Feedback = r.Feedback,
                SubmittedAt = r.CreatedAt,
                Direction = "StudentToTeacher",
            })
            .ToListAsync(cancellationToken);

        var teacherToStudent = await _context.SessionAttendances
            .AsNoTracking()
            .Where(a => a.CourseScheduleId == courseScheduleId && a.Rating != null)
            .Select(a => new SessionReviewDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student != null && a.Student.User != null
                    ? ((a.Student.User.FirstName ?? "") + " " + (a.Student.User.LastName ?? "")).Trim()
                    : $"Student #{a.StudentId}",
                Rating = (int)Math.Round(a.Rating!.Value, MidpointRounding.AwayFromZero),
                Feedback = a.Note,
                SubmittedAt = a.UpdatedAt ?? a.CreatedAt,
                Direction = "TeacherToStudent",
            })
            .ToListAsync(cancellationToken);

        return studentToTeacher
            .Concat(teacherToStudent)
            .OrderByDescending(r => r.SubmittedAt)
            .ToList();
    }

    private async Task RecalculateTeacherRatingAverageAsync(int teacherId, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return;

        var avg = await _context.TeacherReviews
            .AsNoTracking()
            .Where(r => r.TeacherId == teacherId && r.IsApproved)
            .AverageAsync(r => (decimal?)r.Rating, cancellationToken) ?? 0m;

        teacher.RatingAverage = Math.Round(avg, 2, MidpointRounding.AwayFromZero);
        await _teacherRepository.SaveChangesAsync();
    }
}
