using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<RequestCourseEnrollmentCommand, Response<EnrollmentRequestDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly IGuardianRepository _guardianRepository;

    public RequestCourseEnrollmentCommandHandler(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        IGuardianRepository guardianRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _requestRepository = requestRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _guardianRepository = guardianRepository;
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

        // 2. Validate StudentIds — each must be in the owned set
        var studentIds = dto.StudentIds.Distinct().ToList();
        if (studentIds.Count == 0)
            return BadRequest<EnrollmentRequestDetailDto>("At least one student is required.");

        foreach (var sid in studentIds)
        {
            if (!ownedStudentIds.Contains(sid))
                return BadRequest<EnrollmentRequestDetailDto>($"You are not authorized to enroll student {sid}.");
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

        // 6. Validate selected availabilities
        var selectedAvailabilityIds = (dto.SelectedAvailabilityIds ?? new List<int>()).Distinct().ToList();
        if (selectedAvailabilityIds.Count == 0)
            return BadRequest<EnrollmentRequestDetailDto>("At least one selected availability is required.");

        var selectedAvailabilities = await _teacherAvailabilityRepository.GetTableNoTracking()
            .Where(a => selectedAvailabilityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        if (selectedAvailabilities.Count != selectedAvailabilityIds.Count)
            return BadRequest<EnrollmentRequestDetailDto>("One or more selected availability ids are invalid.");
        if (selectedAvailabilities.Any(a => a.TeacherId != course.TeacherId || !a.IsActive))
            return BadRequest<EnrollmentRequestDetailDto>("Selected availabilities must belong to the course teacher and be active.");

        // 7. Validate flexible/proposed sessions
        var proposedSessions = (dto.ProposedSessions ?? new List<CreateProposedSessionDto>())
            .OrderBy(p => p.SessionNumber)
            .ToList();

        if (course.IsFlexible)
        {
            if (proposedSessions.Count == 0)
                return BadRequest<EnrollmentRequestDetailDto>("ProposedSessions are required for flexible courses.");

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

        // 9. Calculate pricing
        var totalMinutes = course.IsFlexible
            ? proposedSessions.Sum(s => s.DurationMinutes)
            : (course.SessionsCount ?? 0) * (course.SessionDurationMinutes ?? 0);

        if (totalMinutes <= 0)
            return BadRequest<EnrollmentRequestDetailDto>("Total duration must be greater than zero.");

        var estimatedTotalPrice = Math.Round((totalMinutes / 60m) * course.Price, 2, MidpointRounding.AwayFromZero);

        // 10. Create entity
        var enrollmentRequest = new Qalam.Data.Entity.Course.CourseEnrollmentRequest
        {
            CourseId = dto.CourseId,
            RequestedByUserId = request.UserId,
            TeachingModeId = course.TeachingModeId,
            Status = RequestStatus.Pending,
            Notes = dto.Notes != null && dto.Notes.Length > 400 ? dto.Notes.Substring(0, 400) : dto.Notes,
            TotalMinutes = totalMinutes,
            EstimatedTotalPrice = estimatedTotalPrice
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
            SelectedAvailabilityIds = enrollmentRequest.SelectedAvailabilities.Select(a => a.TeacherAvailabilityId).ToList(),
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
                .ToList()
        };

        return Success(entity: result);
    }
}
