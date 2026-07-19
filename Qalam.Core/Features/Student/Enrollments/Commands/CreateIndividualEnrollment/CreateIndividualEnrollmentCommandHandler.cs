using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CreateIndividualEnrollment;

public class CreateIndividualEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<CreateIndividualEnrollmentCommand, Response<CreateIndividualEnrollmentResultDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly ITeacherAvailabilityCalendarService _availabilityCalendar;
    private readonly IContentUnitRepository _contentUnitRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly EnrollmentSettings _settings;

    public CreateIndividualEnrollmentCommandHandler(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        IGuardianRepository guardianRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        ITeacherAvailabilityCalendarService availabilityCalendar,
        IContentUnitRepository contentUnitRepository,
        ILessonRepository lessonRepository,
        IOptions<EnrollmentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _guardianRepository = guardianRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _availabilityCalendar = availabilityCalendar;
        _contentUnitRepository = contentUnitRepository;
        _lessonRepository = lessonRepository;
        _settings = settings.Value;
    }

    public async Task<Response<CreateIndividualEnrollmentResultDto>> Handle(
        CreateIndividualEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;

        var ownedStudentIds = new HashSet<int>();
        var ownStudent = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (ownStudent != null)
            ownedStudentIds.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            foreach (var child in children)
                ownedStudentIds.Add(child.Id);
        }

        if (ownedStudentIds.Count == 0)
            return NotFound<CreateIndividualEnrollmentResultDto>("No student profile found for this user.");

        var studentIdsFromDto = (dto.StudentIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        int studentId;
        if (studentIdsFromDto.Count == 0)
        {
            if (ownStudent == null)
                return BadRequest<CreateIndividualEnrollmentResultDto>(
                    "Specify studentIds for the learner to enroll. Your account has no student profile to infer self-enrollment.");
            studentId = ownStudent.Id;
        }
        else if (studentIdsFromDto.Count == 1)
        {
            studentId = studentIdsFromDto[0];
            if (!ownedStudentIds.Contains(studentId))
                return BadRequest<CreateIndividualEnrollmentResultDto>(
                    $"You are not authorized to enroll student {studentId}.");
        }
        else
        {
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "Individual enrollment requires exactly one student.");
        }

        if ((dto.InvitedStudentIds ?? new List<int>()).Count > 0)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "Invited students are not allowed for Individual enrollment. Use EnrollmentRequests for Group.");

        var course = await _courseRepository.GetByIdWithDetailsAsync(dto.CourseId);
        if (course == null)
            return BadRequest<CreateIndividualEnrollmentResultDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return BadRequest<CreateIndividualEnrollmentResultDto>("Course is not available for enrollment.");
        if (course.IsFlexible)
            return BadRequest<CreateIndividualEnrollmentResultDto>("Flexible courses are not supported.");

        var isGroupCourse = string.Equals(
            course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);
        if (isGroupCourse)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "Group courses must use POST /Student/EnrollmentRequests.");

        var hasPendingPayment = await _enrollmentRepository.GetTableNoTracking()
            .AnyAsync(e => e.CourseId == dto.CourseId
                           && e.OwnerUserId == request.UserId
                           && e.EnrollmentStatus == EnrollmentStatus.PendingPayment,
                cancellationToken);
        if (hasPendingPayment)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "You already have a pending-payment enrollment for this course.");

        var selectedSlots = (dto.SelectedSessionSlots ?? new List<SelectedSessionSlotDto>())
            .OrderBy(s => s.SessionNumber)
            .ToList();
        if (selectedSlots.Count == 0)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "At least one session date selection is required.");

        if ((dto.ProposedSessions ?? new List<CreateProposedSessionDto>()).Count > 0)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "ProposedSessions are not allowed for Fixed courses.");

        for (var i = 0; i < selectedSlots.Count; i++)
        {
            if (selectedSlots[i].SessionNumber != i + 1)
                return BadRequest<CreateIndividualEnrollmentResultDto>(
                    "Session numbers must be sequential starting from 1.");
        }

        var duplicateKeys = selectedSlots
            .Select(s => (s.Date, s.TeacherAvailabilityId))
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicateKeys.Count > 0)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "Duplicate selections for the same date and time slot are not allowed.");

        HashSet<int>? allowedUnitIds = null;
        if (dto.TeacherSubjectId.HasValue)
        {
            if (dto.TeacherSubjectId.Value != course.TeacherSubjectId)
                return BadRequest<CreateIndividualEnrollmentResultDto>(
                    "teacherSubjectId must match the course's teacher subject.");

            allowedUnitIds = (course.TeacherSubject?.TeacherSubjectUnits
                              ?? new List<Qalam.Data.Entity.Teacher.TeacherSubjectUnit>())
                .Select(u => u.UnitId)
                .ToHashSet();
        }

        for (var i = 0; i < selectedSlots.Count; i++)
        {
            var unitErr = await ValidateSessionUnitsAsync(
                $"Slot {i + 1}", selectedSlots[i].Units, allowedUnitIds, cancellationToken);
            if (unitErr != null)
                return BadRequest<CreateIndividualEnrollmentResultDto>(unitErr);
        }

        var selectedAvailabilityIds = selectedSlots.Select(s => s.TeacherAvailabilityId).Distinct().ToList();
        var selectedAvailabilities = await _teacherAvailabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .Include(a => a.DayOfWeek)
            .Where(a => selectedAvailabilityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        if (selectedAvailabilities.Count != selectedAvailabilityIds.Count)
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "One or more selected availability ids are invalid.");
        if (selectedAvailabilities.Any(a => a.TeacherId != course.TeacherId || !a.IsActive))
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                "Selected availabilities must belong to the course teacher and be active.");

        var availabilityById = selectedAvailabilities.ToDictionary(a => a.Id);

        var totalMinutes = FixedCourseTotalMinutes(course);
        if (totalMinutes <= 0)
            return BadRequest<CreateIndividualEnrollmentResultDto>("Total duration must be greater than zero.");

        var amountDue = Math.Round((totalMinutes / 60m) * course.Price, 2, MidpointRounding.AwayFromZero);

        var calendarPairs = selectedSlots
            .Select(s => (s.Date, s.TeacherAvailabilityId))
            .ToList();
        var statuses = await _availabilityCalendar.GetSlotStatusesAsync(
            course.TeacherId, calendarPairs, cancellationToken);
        foreach (var slot in selectedSlots)
        {
            if (!statuses.TryGetValue((slot.Date, slot.TeacherAvailabilityId), out var st)
                || st != AvailabilitySlotStatus.Free)
            {
                return BadRequest<CreateIndividualEnrollmentResultDto>(
                    $"The chosen slot on {slot.Date:yyyy-MM-dd} is no longer available. Refresh availability and pick another date.");
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedStart = dto.PreferredStartDate ?? today;
        if (resolvedStart < today)
            resolvedStart = today;

        const int defaultPersistedWindowYears = 2;
        var persistedEnd = dto.PreferredEndDate ?? resolvedStart.AddYears(defaultPersistedWindowYears);
        DateOnly? previewHardEnd = dto.PreferredEndDate;

        if (previewHardEnd.HasValue)
        {
            foreach (var s in selectedSlots)
            {
                if (s.Date > previewHardEnd.Value)
                    return BadRequest<CreateIndividualEnrollmentResultDto>(
                        $"All session dates must be on or before PreferredEndDate ({previewHardEnd.Value:yyyy-MM-dd}).");
            }
        }

        foreach (var s in selectedSlots)
        {
            if (s.Date < today)
                return BadRequest<CreateIndividualEnrollmentResultDto>("Session dates cannot be in the past.");
        }

        var stubRequest = new CourseEnrollmentRequest { ProposedSessions = [] };
        var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
            course.TeacherId, resolvedStart, persistedEnd);
        var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
            resolvedStart, persistedEnd, selectedAvailabilityIds, cancellationToken);
        var selectionsOrdered = selectedSlots.Select(s => (s.Date, s.TeacherAvailabilityId)).ToList();
        var preview = _scheduleGenerator.PreviewExplicit(
            course,
            stubRequest,
            selectionsOrdered,
            availabilityById,
            blockedExceptions,
            existingScheduledSlots,
            previewHardEnd);

        if (preview.Conflicts.Count > 0)
        {
            var firstFew = string.Join("; ",
                preview.Conflicts.Take(3).Select(c => $"session {c.SessionNumber}: {c.Date:yyyy-MM-dd}"));
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                $"Scheduling conflict: {firstFew}. Pick different dates or slots.");
        }

        if (!preview.FitsInWindow)
        {
            return BadRequest<CreateIndividualEnrollmentResultDto>(
                previewHardEnd.HasValue
                    ? $"One or more sessions fall after the preferred end date ({previewHardEnd.Value:yyyy-MM-dd})."
                    : "Schedule validation failed for the preferred date window.");
        }

        var now = DateTime.UtcNow;
        var isFree = amountDue <= 0;
        var enrollment = new Enrollment
        {
            Source = EnrollmentSource.CourseRequest,
            CourseId = course.Id,
            EnrollmentRequestId = null,
            Kind = EnrollmentKind.Individual,
            LeaderStudentId = null,
            ApprovedByTeacherId = course.TeacherId,
            ApprovedAt = now,
            PaymentDeadline = isFree ? null : now.AddHours(_settings.PaymentDeadlineHours),
            EnrollmentStatus = isFree ? EnrollmentStatus.Active : EnrollmentStatus.PendingPayment,
            ActivatedAt = isFree ? now : null,
            AmountDue = amountDue,
            OwnerUserId = request.UserId,
            PreferredStartDate = resolvedStart,
            PreferredEndDate = persistedEnd,
            Participants =
            [
                new EnrollmentParticipant
                {
                    StudentId = studentId,
                    PaymentStatus = isFree ? PaymentStatus.Succeeded : PaymentStatus.Pending,
                    PaidAt = isFree ? now : null
                }
            ],
            SelectedSessionSlots = selectedSlots.Select(s => new EnrollmentSelectedSessionSlot
            {
                SessionNumber = s.SessionNumber,
                TeacherAvailabilityId = s.TeacherAvailabilityId,
                SessionDate = s.Date,
                Units = (s.Units ?? new List<EnrollmentSessionUnitDto>())
                    .Select(u => new EnrollmentSelectedSessionSlotUnit
                    {
                        ContentUnitId = u.ContentUnitId,
                        LessonId = u.LessonId
                    })
                    .ToList()
            }).ToList()
        };

        if (isFree)
        {
            Dictionary<int, int>? courseSessionIdByNumber = null;
            if (course.Sessions != null)
            {
                courseSessionIdByNumber = course.Sessions
                    .GroupBy(cs => cs.SessionNumber)
                    .ToDictionary(g => g.Key, g => g.First().Id);
            }

            foreach (var s in preview.Slots)
            {
                int? courseSessionId = null;
                if (courseSessionIdByNumber != null
                    && courseSessionIdByNumber.TryGetValue(s.SessionNumber, out var sid))
                {
                    courseSessionId = sid;
                }

                enrollment.CourseSchedules.Add(new CourseSchedule
                {
                    Date = s.Date,
                    TeacherAvailabilityId = s.TeacherAvailabilityId,
                    DurationMinutes = s.DurationMinutes,
                    TeachingModeId = course.TeachingModeId,
                    CourseSessionId = courseSessionId,
                    LocationId = null,
                    Status = ScheduleStatus.Scheduled
                });
            }
        }

        var transaction = await _enrollmentRepository.BeginTransactionAsync();
        try
        {
            await _enrollmentRepository.AddAsync(enrollment);
            await _enrollmentRepository.SaveChangesAsync();
            await _enrollmentRepository.CommitAsync();
        }
        catch
        {
            await _enrollmentRepository.RollBackAsync();
            throw;
        }

        var payParticipantId = enrollment.Participants.FirstOrDefault()?.Id;
        var canPay = !isFree
                     && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
                     && payParticipantId.HasValue;

        return Success(entity: new CreateIndividualEnrollmentResultDto
        {
            Id = enrollment.Id,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            AmountDue = enrollment.AmountDue,
            PaymentDeadline = enrollment.PaymentDeadline,
            PayParticipantId = canPay ? payParticipantId : null,
            CanPay = canPay,
            CourseTitle = course.Title
        });
    }

    private async Task<string?> ValidateSessionUnitsAsync(
        string sessionLabel,
        IReadOnlyList<EnrollmentSessionUnitDto> units,
        HashSet<int>? allowedUnitIds,
        CancellationToken cancellationToken)
    {
        if (units == null || units.Count == 0)
            return null;

        foreach (var u in units)
        {
            var hasContentUnit = u.ContentUnitId.HasValue;
            var hasLesson = u.LessonId.HasValue;
            if (hasContentUnit == hasLesson)
                return $"{sessionLabel}: each unit row must set exactly one of contentUnitId or lessonId.";

            if (allowedUnitIds is not null)
            {
                if (hasContentUnit)
                {
                    if (!allowedUnitIds.Contains(u.ContentUnitId!.Value))
                        return $"{sessionLabel}: contentUnitId {u.ContentUnitId} is outside this teacher's repertoire.";
                }
                else
                {
                    var lesson = await _lessonRepository.GetByIdAsync(u.LessonId!.Value);
                    if (lesson == null || !allowedUnitIds.Contains(lesson.UnitId))
                        return $"{sessionLabel}: lessonId {u.LessonId} is outside this teacher's repertoire.";
                }
            }
            else
            {
                if (hasContentUnit)
                {
                    var unit = await _contentUnitRepository.GetByIdAsync(u.ContentUnitId!.Value);
                    if (unit == null)
                        return $"{sessionLabel}: contentUnitId {u.ContentUnitId} does not exist.";
                }
                else
                {
                    var lesson = await _lessonRepository.GetByIdAsync(u.LessonId!.Value);
                    if (lesson == null)
                        return $"{sessionLabel}: lessonId {u.LessonId} does not exist.";
                }
            }
        }

        return null;
    }

    private static int FixedCourseTotalMinutes(Course course)
    {
        var sessionCountNav = course.Sessions?.Count ?? 0;
        var sumSessions = sessionCountNav > 0 ? course.Sessions!.Sum(s => s.DurationMinutes) : 0;
        var countForUniform = course.SessionsCount ?? sessionCountNav;
        var uniformProduct = countForUniform * (course.SessionDurationMinutes ?? 0);
        if (sumSessions > 0)
            return sumSessions;
        return uniformProduct;
    }
}
