using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<RequestCourseEnrollmentCommand, Response<EnrollmentRequestDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly ITeacherAvailabilityCalendarService _availabilityCalendar;

    public RequestCourseEnrollmentCommandHandler(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        IGuardianRepository guardianRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        ITeacherAvailabilityCalendarService availabilityCalendar,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _requestRepository = requestRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _guardianRepository = guardianRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _availabilityCalendar = availabilityCalendar;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        RequestCourseEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;

        // 1. Build the set of student IDs the user owns (self + guardian's children)
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
            return NotFound<EnrollmentRequestDetailDto>("No student profile found for this user.");

        // 2. Resolve StudentIds — empty means enroll the requester only (requires a linked student profile)
        var studentIdsFromDto = (dto.StudentIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        List<int> studentIds;
        if (studentIdsFromDto.Count == 0)
        {
            if (ownStudent == null)
                return BadRequest<EnrollmentRequestDetailDto>(
                    "Specify studentIds for the learners to enroll. Include child ids if you are enrolling only children; your account has no student profile to infer self-enrollment.");

            studentIds = new List<int> { ownStudent.Id };
        }
        else
        {
            studentIds = studentIdsFromDto;
            foreach (var sid in studentIds)
            {
                if (!ownedStudentIds.Contains(sid))
                    return BadRequest<EnrollmentRequestDetailDto>($"You are not authorized to enroll student {sid}.");
            }
        }

        // 3. Validate InvitedStudentIds
        var invitedIds = (dto.InvitedStudentIds ?? new List<int>()).Distinct().ToList();
        var overlap = invitedIds.Intersect(studentIds).ToList();
        if (overlap.Count > 0)
            return BadRequest<EnrollmentRequestDetailDto>("Invited students cannot overlap with your own students.");

        var ownedOverlap = invitedIds.Where(id => ownedStudentIds.Contains(id)).ToList();
        if (ownedOverlap.Count > 0)
            return BadRequest<EnrollmentRequestDetailDto>("You cannot invite your own students. Add them to StudentIds instead.");

        if (invitedIds.Count > 0)
        {
            var existingInvited = await _studentRepository.GetTableNoTracking()
                .Where(s => invitedIds.Contains(s.Id) && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            if (existingInvited.Count != invitedIds.Count)
                return BadRequest<EnrollmentRequestDetailDto>("One or more invited students are invalid.");
        }

        // 4. Fetch and validate course
        var course = await _courseRepository.GetByIdWithDetailsAsync(dto.CourseId);
        if (course == null)
            return BadRequest<EnrollmentRequestDetailDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return BadRequest<EnrollmentRequestDetailDto>("Course is not available for enrollment.");

        // 5. Duplicate check — same user + course + pending
        var hasPending = await _requestRepository.GetTableNoTracking()
            .AnyAsync(r => r.CourseId == dto.CourseId
                        && r.RequestedByUserId == request.UserId
                        && r.Status == RequestStatus.Pending, cancellationToken);
        if (hasPending)
            return BadRequest<EnrollmentRequestDetailDto>("You already have a pending enrollment request for this course.");

        // 6. Validate selected session dates (from availability API cells)
        var selectedSlots = (dto.SelectedSessionSlots ?? new List<SelectedSessionSlotDto>())
            .OrderBy(s => s.SessionNumber)
            .ToList();
        if (selectedSlots.Count == 0)
            return BadRequest<EnrollmentRequestDetailDto>("At least one session date selection is required.");

        // 7. Validate flexible/proposed sessions
        var proposedSessions = (dto.ProposedSessions ?? new List<CreateProposedSessionDto>())
            .OrderBy(p => p.SessionNumber)
            .ToList();

        if (course.IsFlexible && proposedSessions.Count > 0)
        {
            for (var i = 0; i < proposedSessions.Count; i++)
            {
                if (proposedSessions[i].SessionNumber != i + 1)
                    return BadRequest<EnrollmentRequestDetailDto>("Proposed session numbers must be sequential starting from 1.");
                if (proposedSessions[i].DurationMinutes <= 0)
                    return BadRequest<EnrollmentRequestDetailDto>("Proposed session duration must be greater than zero.");
            }
        }
        else if (proposedSessions.Count > 0)
        {
            return BadRequest<EnrollmentRequestDetailDto>("ProposedSessions are not allowed for non-flexible courses.");
        }

        // 8. Group vs individual validation
        var isGroupCourse = string.Equals(course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);
        var totalMembers = studentIds.Count + invitedIds.Count;

        if (isGroupCourse)
        {
            if (!course.MaxStudents.HasValue || course.MaxStudents.Value < 2)
                return BadRequest<EnrollmentRequestDetailDto>("Group courses must define MaxStudents >= 2.");

            if (totalMembers > course.MaxStudents.Value)
                return BadRequest<EnrollmentRequestDetailDto>("Group size exceeds MaxStudents.");
        }
        else
        {
            if (studentIds.Count != 1)
                return BadRequest<EnrollmentRequestDetailDto>("Individual courses require exactly one student.");
            if (invitedIds.Count > 0)
                return BadRequest<EnrollmentRequestDetailDto>("Invited students are only allowed for group courses.");
        }

        for (var i = 0; i < selectedSlots.Count; i++)
        {
            if (selectedSlots[i].SessionNumber != i + 1)
                return BadRequest<EnrollmentRequestDetailDto>("Session numbers must be sequential starting from 1.");
        }

        var duplicateKeys = selectedSlots
            .Select(s => (s.Date, s.TeacherAvailabilityId))
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicateKeys.Count > 0)
            return BadRequest<EnrollmentRequestDetailDto>("Duplicate selections for the same date and time slot are not allowed.");

        var selectedAvailabilityIds = selectedSlots.Select(s => s.TeacherAvailabilityId).Distinct().ToList();
        var selectedAvailabilities = await _teacherAvailabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .Include(a => a.DayOfWeek)
            .Where(a => selectedAvailabilityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        if (selectedAvailabilities.Count != selectedAvailabilityIds.Count)
            return BadRequest<EnrollmentRequestDetailDto>("One or more selected availability ids are invalid.");
        if (selectedAvailabilities.Any(a => a.TeacherId != course.TeacherId || !a.IsActive))
            return BadRequest<EnrollmentRequestDetailDto>("Selected availabilities must belong to the course teacher and be active.");

        var availabilityById = selectedAvailabilities.ToDictionary(a => a.Id);

        var totalMinutes = !course.IsFlexible
            ? FixedCourseTotalMinutes(course)
            : proposedSessions.Count > 0
                ? proposedSessions.Sum(s => s.DurationMinutes)
                : selectedSlots.Sum(s =>
                    availabilityById.TryGetValue(s.TeacherAvailabilityId, out var a) && a.TimeSlot != null
                        ? a.TimeSlot.ResolveDurationMinutes()
                        : 0);

        if (totalMinutes <= 0)
        {
            var hint = course.IsFlexible && proposedSessions.Count == 0
                ? " For flexible enrollment without proposed sessions, each chosen availability must use a time band with duration greater than zero."
                : string.Empty;
            return BadRequest<EnrollmentRequestDetailDto>("Total duration must be greater than zero." + hint);
        }

        var estimatedTotalPrice = Math.Round((totalMinutes / 60m) * course.Price, 2, MidpointRounding.AwayFromZero);

        var calendarPairs = selectedSlots
            .Select(s => (s.Date, s.TeacherAvailabilityId))
            .ToList();
        var statuses = await _availabilityCalendar.GetSlotStatusesAsync(course.TeacherId, calendarPairs, cancellationToken);
        foreach (var slot in selectedSlots)
        {
            if (!statuses.TryGetValue((slot.Date, slot.TeacherAvailabilityId), out var st) || st != AvailabilitySlotStatus.Free)
            {
                return BadRequest<EnrollmentRequestDetailDto>(
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
                    return BadRequest<EnrollmentRequestDetailDto>(
                        $"All session dates must be on or before PreferredEndDate ({previewHardEnd.Value:yyyy-MM-dd}).");
            }
        }

        foreach (var s in selectedSlots)
        {
            if (s.Date < today)
                return BadRequest<EnrollmentRequestDetailDto>("Session dates cannot be in the past.");
        }

        var transientRequest = new Qalam.Data.Entity.Course.CourseEnrollmentRequest
        {
            ProposedSessions = proposedSessions
                .Select(p => new Qalam.Data.Entity.Course.CourseRequestProposedSession
                {
                    SessionNumber = p.SessionNumber,
                    DurationMinutes = p.DurationMinutes,
                    Title = p.Title,
                    Notes = p.Notes
                })
                .ToList()
        };

        var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
            course.TeacherId,
            resolvedStart,
            persistedEnd);

        var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
            resolvedStart,
            persistedEnd,
            selectedAvailabilityIds,
            cancellationToken);

        var selectionsOrdered = selectedSlots.Select(s => (s.Date, s.TeacherAvailabilityId)).ToList();
        var preview = _scheduleGenerator.PreviewExplicit(
            course,
            transientRequest,
            selectionsOrdered,
            availabilityById,
            blockedExceptions,
            existingScheduledSlots,
            previewHardEnd);

        if (preview.Conflicts.Count > 0)
        {
            var firstFew = string.Join("; ",
                preview.Conflicts.Take(3).Select(c => $"session {c.SessionNumber}: {c.Date:yyyy-MM-dd}"));
            return BadRequest<EnrollmentRequestDetailDto>(
                $"Scheduling conflict: {firstFew}. Pick different dates or slots.");
        }

        if (!preview.FitsInWindow)
        {
            return BadRequest<EnrollmentRequestDetailDto>(
                previewHardEnd.HasValue
                    ? $"One or more sessions fall after the preferred end date ({previewHardEnd.Value:yyyy-MM-dd})."
                    : "Schedule validation failed for the preferred date window.");
        }

        // 10. Create entity
        var enrollmentRequest = new Qalam.Data.Entity.Course.CourseEnrollmentRequest
        {
            CourseId = dto.CourseId,
            RequestedByUserId = request.UserId,
            TeachingModeId = course.TeachingModeId,
            Status = RequestStatus.Pending,
            Notes = dto.Notes != null && dto.Notes.Length > 400 ? dto.Notes.Substring(0, 400) : dto.Notes,
            TotalMinutes = totalMinutes,
            EstimatedTotalPrice = estimatedTotalPrice,
            PreferredStartDate = resolvedStart,
            PreferredEndDate = persistedEnd
        };

        // Own students — auto-confirmed
        var groupMembers = studentIds
            .Select(id => new Qalam.Data.Entity.Course.CourseRequestGroupMember
            {
                StudentId = id,
                MemberType = GroupMemberType.Own,
                InvitedByStudentId = null,
                ConfirmationStatus = GroupMemberConfirmationStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow,
                ConfirmedByUserId = request.UserId
            })
            .ToList();

        // Invited students — pending confirmation
        groupMembers.AddRange(invitedIds
            .Select(id => new Qalam.Data.Entity.Course.CourseRequestGroupMember
            {
                StudentId = id,
                MemberType = GroupMemberType.Invited,
                InvitedByStudentId = null,
                ConfirmationStatus = GroupMemberConfirmationStatus.Pending
            }));

        enrollmentRequest.GroupMembers = groupMembers;

        enrollmentRequest.SelectedAvailabilities = selectedAvailabilityIds
            .Select(id => new Qalam.Data.Entity.Course.CourseRequestSelectedAvailability
            {
                TeacherAvailabilityId = id
            })
            .ToList();

        enrollmentRequest.SelectedSessionSlots = selectedSlots
            .Select(s => new Qalam.Data.Entity.Course.CourseRequestSelectedSessionSlot
            {
                SessionNumber = s.SessionNumber,
                TeacherAvailabilityId = s.TeacherAvailabilityId,
                SessionDate = s.Date
            })
            .ToList();

        enrollmentRequest.ProposedSessions = proposedSessions
            .Select(p => new Qalam.Data.Entity.Course.CourseRequestProposedSession
            {
                SessionNumber = p.SessionNumber,
                DurationMinutes = p.DurationMinutes,
                Title = p.Title,
                Notes = p.Notes
            })
            .ToList();

        await _requestRepository.AddAsync(enrollmentRequest);
        await _requestRepository.SaveChangesAsync();

        // 11. Build response
        var descriptionShort = course.Description != null && course.Description.Length > 150
            ? course.Description.Substring(0, 150) + "..."
            : course.Description;

        var result = new EnrollmentRequestDetailDto
        {
            Id = enrollmentRequest.Id,
            CourseId = enrollmentRequest.CourseId,
            CourseTitle = course.Title,
            CourseDescriptionShort = descriptionShort,
            CoursePrice = course.Price,
            TeachingModeId = enrollmentRequest.TeachingModeId,
            TeachingModeNameEn = course.TeachingMode?.NameEn,
            SessionTypeId = course.SessionTypeId,
            SessionTypeNameEn = course.SessionType?.NameEn,
            Status = enrollmentRequest.Status,
            CreatedAt = enrollmentRequest.CreatedAt,
            Notes = enrollmentRequest.Notes,
            TotalMinutes = enrollmentRequest.TotalMinutes,
            EstimatedTotalPrice = enrollmentRequest.EstimatedTotalPrice,
            SelectedSessionSlots = selectedSlots
                .Select(s => new SelectedSessionSlotDto
                {
                    SessionNumber = s.SessionNumber,
                    TeacherAvailabilityId = s.TeacherAvailabilityId,
                    Date = s.Date
                })
                .ToList(),
            GroupMembers = enrollmentRequest.GroupMembers
                .Select(g => new EnrollmentRequestGroupMemberDto
                {
                    StudentId = g.StudentId,
                    MemberType = g.MemberType,
                    ConfirmationStatus = g.ConfirmationStatus,
                    ConfirmedAt = g.ConfirmedAt,
                    ConfirmedByUserId = g.ConfirmedByUserId
                })
                .ToList(),
            ProposedSessions = enrollmentRequest.ProposedSessions
                .OrderBy(p => p.SessionNumber)
                .Select(p => new EnrollmentRequestProposedSessionDto
                {
                    SessionNumber = p.SessionNumber,
                    DurationMinutes = p.DurationMinutes,
                    Title = p.Title,
                    Notes = p.Notes
                })
                .ToList(),
            PreferredStartDate = enrollmentRequest.PreferredStartDate,
            PreferredEndDate = enrollmentRequest.PreferredEndDate,
            ProposedScheduleDates = preview.Slots
                .Select(s => new ProposedScheduleSlotDto
                {
                    SessionNumber = s.SessionNumber,
                    Date = s.Date,
                    TeacherAvailabilityId = s.TeacherAvailabilityId,
                    DurationMinutes = s.DurationMinutes,
                    Title = s.Title
                })
                .ToList()
        };

        return Success(entity: result);
    }

    /// <summary>
    /// Non-flexible courses store duration per <see cref="CourseSession"/>; <see cref="Course.SessionDurationMinutes"/> may be null.
    /// Prefer summing sessions when present so pricing matches catalog totals.
    /// </summary>
    private static int FixedCourseTotalMinutes(Qalam.Data.Entity.Course.Course course)
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
