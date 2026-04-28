using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Queries.GetTeacherEnrollmentRequestById;

public class GetTeacherEnrollmentRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherEnrollmentRequestByIdQuery, Response<TeacherEnrollmentRequestDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;

    public GetTeacherEnrollmentRequestByIdQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _requestRepository = requestRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
    }

    public async Task<Response<TeacherEnrollmentRequestDetailDto>> Handle(
        GetTeacherEnrollmentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherEnrollmentRequestDetailDto>("Teacher profile not found.");

        var enrollmentRequest = await _requestRepository.GetTableNoTracking()
            .Include(r => r.Course).ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .Include(r => r.Course).ThenInclude(c => c.Sessions)
            .Include(r => r.RequestedByUser)
            .Include(r => r.GroupMembers).ThenInclude(gm => gm.Student).ThenInclude(s => s.User)
            .Include(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(r => r.ProposedSessions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (enrollmentRequest == null || enrollmentRequest.Course.TeacherId != teacher.Id)
            return NotFound<TeacherEnrollmentRequestDetailDto>("Enrollment request not found.");

        var dto = new TeacherEnrollmentRequestDetailDto
        {
            Id = enrollmentRequest.Id,
            CourseId = enrollmentRequest.CourseId,
            CourseTitle = enrollmentRequest.Course.Title,
            RequestedByUserName = enrollmentRequest.RequestedByUser != null
                ? (enrollmentRequest.RequestedByUser.FirstName + " " + enrollmentRequest.RequestedByUser.LastName).Trim()
                : null,
            Status = enrollmentRequest.Status,
            CreatedAt = enrollmentRequest.CreatedAt,
            TotalMinutes = enrollmentRequest.TotalMinutes,
            EstimatedTotalPrice = enrollmentRequest.EstimatedTotalPrice,
            TeachingModeNameEn = enrollmentRequest.Course.TeachingMode?.NameEn,
            SessionTypeNameEn = enrollmentRequest.Course.SessionType?.NameEn,
            Notes = enrollmentRequest.Notes,
            RejectionReason = enrollmentRequest.RejectionReason,
            PreferredStartDate = enrollmentRequest.PreferredStartDate,
            PreferredEndDate = enrollmentRequest.PreferredEndDate,
            SelectedAvailabilityIds = enrollmentRequest.SelectedAvailabilities.Select(a => a.TeacherAvailabilityId).ToList(),
            GroupMembers = enrollmentRequest.GroupMembers.Select(gm => new TeacherEnrollmentRequestGroupMemberDto
            {
                StudentId = gm.StudentId,
                StudentName = gm.Student?.User != null
                    ? (gm.Student.User.FirstName + " " + gm.Student.User.LastName).Trim()
                    : null,
                MemberType = gm.MemberType,
                ConfirmationStatus = gm.ConfirmationStatus,
                ConfirmedAt = gm.ConfirmedAt
            }).ToList(),
            ProposedSessions = enrollmentRequest.ProposedSessions
                .OrderBy(p => p.SessionNumber)
                .Select(p => new TeacherEnrollmentRequestProposedSessionDto
                {
                    SessionNumber = p.SessionNumber,
                    DurationMinutes = p.DurationMinutes,
                    Title = p.Title,
                    Notes = p.Notes
                }).ToList()
        };

        // Concrete proposed dates the teacher will see while approving.
        var slots = enrollmentRequest.SelectedAvailabilities
            .Select(sa => sa.TeacherAvailability)
            .Where(ta => ta != null)
            .ToList();

        if (slots.Count > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var effectiveStart = enrollmentRequest.PreferredStartDate < today ? today : enrollmentRequest.PreferredStartDate;

            var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
                teacher.Id,
                effectiveStart,
                enrollmentRequest.PreferredEndDate);

            var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
                effectiveStart,
                enrollmentRequest.PreferredEndDate,
                slots.Select(s => s.Id).ToList(),
                cancellationToken);

            var preview = _scheduleGenerator.Preview(
                enrollmentRequest.Course,
                enrollmentRequest,
                slots,
                blockedExceptions,
                existingScheduledSlots,
                effectiveStart,
                enrollmentRequest.PreferredEndDate);

            dto.ProposedScheduleDates = preview.Slots
                .Select(s => new ProposedScheduleSlotDto
                {
                    SessionNumber = s.SessionNumber,
                    Date = s.Date,
                    TeacherAvailabilityId = s.TeacherAvailabilityId,
                    DurationMinutes = s.DurationMinutes,
                    Title = s.Title
                })
                .ToList();
        }

        return Success(entity: dto);
    }
}
