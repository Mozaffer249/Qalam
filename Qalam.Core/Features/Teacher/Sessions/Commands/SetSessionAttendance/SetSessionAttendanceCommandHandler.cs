using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.SetSessionAttendance;

public class SetSessionAttendanceCommandHandler : ResponseHandler,
    IRequestHandler<SetSessionAttendanceCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;

    public SetSessionAttendanceCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Response<string>> Handle(SetSessionAttendanceCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<string>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<string>("This session does not belong to you.");

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest<string>("At least one attendance item is required.");

        var isOnline = string.Equals(schedule.TeachingMode?.Code, "online", StringComparison.OrdinalIgnoreCase);

        var hasReviewFields = request.Items.Any(i => i.Rating.HasValue || !string.IsNullOrWhiteSpace(i.Note));
        if (hasReviewFields && schedule.Status != ScheduleStatus.Completed)
            return BadRequest<string>("Student ratings and notes can only be set after the session is completed.");

        var participantIds = schedule.Enrollment.Participants
            .Select(p => p.StudentId)
            .ToHashSet();

        var byStudent = schedule.Attendances
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var item in request.Items)
        {
            if (!participantIds.Contains(item.StudentId))
                return BadRequest<string>($"Student {item.StudentId} is not a participant in this enrollment.");

            if (!Enum.IsDefined(typeof(SessionAttendanceStatus), item.Status)
                || item.Status == SessionAttendanceStatus.Pending)
                return BadRequest<string>($"Invalid attendance status for student {item.StudentId}.");

            if (item.Rating is < 0 or > 5)
                return BadRequest<string>($"Rating for student {item.StudentId} must be between 0 and 5.");

            if (isOnline)
            {
                if (!byStudent.TryGetValue(item.StudentId, out var existingOnline))
                    return BadRequest<string>("Attendance for online sessions is automatic from the live room.");

                if (existingOnline.Status != item.Status)
                    return BadRequest<string>("Attendance for online sessions is automatic; teachers cannot change marks.");

                // Reviews only after complete (already gated above).
                existingOnline.Rating = item.Rating;
                existingOnline.Note = item.Note;
                continue;
            }

            if (byStudent.TryGetValue(item.StudentId, out var existing))
            {
                existing.Status = item.Status;
                existing.Rating = item.Rating;
                existing.Note = item.Note;
                existing.IsAutoResolved = false;
            }
            else
            {
                var created = new SessionAttendance
                {
                    CourseScheduleId = schedule.Id,
                    StudentId = item.StudentId,
                    Status = item.Status,
                    Rating = item.Rating,
                    Note = item.Note,
                    IsAutoResolved = false,
                };
                schedule.Attendances.Add(created);
                byStudent[item.StudentId] = created;
            }
        }

        await _scheduleRepository.SaveChangesAsync();
        return Success(entity: "Attendance updated.");
    }
}
