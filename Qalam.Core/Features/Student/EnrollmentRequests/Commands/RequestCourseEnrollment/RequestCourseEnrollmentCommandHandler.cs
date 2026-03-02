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
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;

    public RequestCourseEnrollmentCommandHandler(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ITeachingModeRepository teachingModeRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _requestRepository = requestRepository;
        _teachingModeRepository = teachingModeRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        RequestCourseEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<EnrollmentRequestDetailDto>("Student not found.");

        var dto = request.Data;
        var selectedAvailabilityIds = (dto.SelectedAvailabilityIds ?? new List<int>()).Distinct().ToList();
        var groupMemberIds = (dto.GroupMemberStudentIds ?? new List<int>()).Distinct().ToList();
        var proposedSessions = (dto.ProposedSessions ?? new List<CreateProposedSessionDto>())
            .OrderBy(p => p.SessionNumber)
            .ToList();

        var course = await _courseRepository.GetByIdWithDetailsAsync(dto.CourseId);
        if (course == null)
            return BadRequest<EnrollmentRequestDetailDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return BadRequest<EnrollmentRequestDetailDto>("Course is not available for enrollment.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            return BadRequest<EnrollmentRequestDetailDto>("Invalid TeachingModeId.");
        if (course.TeachingModeId != dto.TeachingModeId)
            return BadRequest<EnrollmentRequestDetailDto>("Teaching mode does not match the course.");

        var hasPending = await _requestRepository.GetTableNoTracking()
            .AnyAsync(r => r.CourseId == dto.CourseId && r.RequestedByStudentId == student.Id && r.Status == RequestStatus.Pending, cancellationToken);
        if (hasPending)
            return BadRequest<EnrollmentRequestDetailDto>("You already have a pending enrollment request for this course.");

        if (selectedAvailabilityIds.Count == 0)
            return BadRequest<EnrollmentRequestDetailDto>("At least one selected availability is required.");

        var selectedAvailabilities = await _teacherAvailabilityRepository.GetTableNoTracking()
            .Where(a => selectedAvailabilityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        if (selectedAvailabilities.Count != selectedAvailabilityIds.Count)
            return BadRequest<EnrollmentRequestDetailDto>("One or more selected availability ids are invalid.");
        if (selectedAvailabilities.Any(a => a.TeacherId != course.TeacherId || !a.IsActive))
            return BadRequest<EnrollmentRequestDetailDto>("Selected availabilities must belong to the course teacher and be active.");

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

        var isGroupCourse = string.Equals(course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);

        if (isGroupCourse)
        {
            if (!course.MaxStudents.HasValue || course.MaxStudents.Value < 2)
                return BadRequest<EnrollmentRequestDetailDto>("Group courses must define MaxStudents >= 2.");

            if (groupMemberIds.Contains(student.Id))
                return BadRequest<EnrollmentRequestDetailDto>("Requester cannot be included in group members.");

            var totalMembers = 1 + groupMemberIds.Count;
            if (totalMembers > course.MaxStudents.Value)
                return BadRequest<EnrollmentRequestDetailDto>("Group size exceeds MaxStudents.");

            if (groupMemberIds.Count > 0)
            {
                var existingMembers = await _studentRepository.GetTableNoTracking()
                    .Where(s => groupMemberIds.Contains(s.Id) && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);
                if (existingMembers.Count != groupMemberIds.Count)
                    return BadRequest<EnrollmentRequestDetailDto>("One or more group members are invalid.");
            }
        }
        else if (groupMemberIds.Count > 0)
        {
            return BadRequest<EnrollmentRequestDetailDto>("Group members are only allowed for group courses.");
        }

        var totalMinutes = course.IsFlexible
            ? proposedSessions.Sum(s => s.DurationMinutes)
            : (course.SessionsCount ?? 0) * (course.SessionDurationMinutes ?? 0);

        if (totalMinutes <= 0)
            return BadRequest<EnrollmentRequestDetailDto>("Total duration must be greater than zero.");

        var estimatedTotalPrice = Math.Round((totalMinutes / 60m) * course.Price, 2, MidpointRounding.AwayFromZero);

        var enrollmentRequest = new Qalam.Data.Entity.Course.CourseEnrollmentRequest
        {
            CourseId = dto.CourseId,
            RequestedByStudentId = student.Id,
            TeachingModeId = dto.TeachingModeId,
            Status = RequestStatus.Pending,
            Notes = dto.Notes != null && dto.Notes.Length > 400 ? dto.Notes.Substring(0, 400) : dto.Notes,
            TotalMinutes = totalMinutes,
            EstimatedTotalPrice = estimatedTotalPrice
        };

        enrollmentRequest.SelectedAvailabilities = selectedAvailabilityIds
            .Select(id => new Qalam.Data.Entity.Course.CourseRequestSelectedAvailability
            {
                TeacherAvailabilityId = id
            })
            .ToList();

        enrollmentRequest.GroupMembers = groupMemberIds
            .Select(id => new Qalam.Data.Entity.Course.CourseRequestGroupMember
            {
                StudentId = id,
                InvitedByStudentId = student.Id,
                ConfirmationStatus = GroupMemberConfirmationStatus.Pending
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
            TeachingModeNameEn = teachingMode.NameEn,
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
