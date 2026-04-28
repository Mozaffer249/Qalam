using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;

public class GetMyEnrollmentRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestByIdQuery, Response<EnrollmentRequestDetailDto>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestByIdQueryHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _mapper = mapper;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        GetMyEnrollmentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableNoTracking()
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .Include(r => r.Course)
                .ThenInclude(c => c.Sessions)
            .Include(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(r => r.GroupMembers)
            .Include(r => r.ProposedSessions)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.RequestedByUserId == request.UserId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var dto = _mapper.Map<EnrollmentRequestDetailDto>(enrollmentRequest);
        dto.ProposedScheduleDates = await ComputeProposedDatesAsync(enrollmentRequest, cancellationToken);

        return Success(entity: dto);
    }

    private async Task<List<ProposedScheduleSlotDto>> ComputeProposedDatesAsync(
        Qalam.Data.Entity.Course.CourseEnrollmentRequest enrollmentRequest,
        CancellationToken ct)
    {
        var slots = enrollmentRequest.SelectedAvailabilities
            .Select(sa => sa.TeacherAvailability)
            .Where(ta => ta != null)
            .ToList();

        if (slots.Count == 0)
            return new List<ProposedScheduleSlotDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStart = enrollmentRequest.PreferredStartDate < today ? today : enrollmentRequest.PreferredStartDate;

        var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
            enrollmentRequest.Course.TeacherId,
            effectiveStart,
            enrollmentRequest.PreferredEndDate);

        var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
            effectiveStart,
            enrollmentRequest.PreferredEndDate,
            slots.Select(s => s.Id).ToList(),
            ct);

        var preview = _scheduleGenerator.Preview(
            enrollmentRequest.Course,
            enrollmentRequest,
            slots,
            blockedExceptions,
            existingScheduledSlots,
            effectiveStart,
            enrollmentRequest.PreferredEndDate);

        return preview.Slots
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
}
