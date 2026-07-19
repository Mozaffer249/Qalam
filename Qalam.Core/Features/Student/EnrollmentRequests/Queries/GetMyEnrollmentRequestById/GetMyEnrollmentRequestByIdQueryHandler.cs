using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;

public class GetMyEnrollmentRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestByIdQuery, Response<EnrollmentRequestDetailDto>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestByIdQueryHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
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
            .Include(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(r => r.GroupMembers)
                .ThenInclude(gm => gm.Student)
                    .ThenInclude(s => s.User)
            .Include(r => r.ProposedSessions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var isOwner = enrollmentRequest.RequestedByUserId == request.UserId;

        var viewerStudentIds = await _studentRepository.GetTableNoTracking()
            .Where(s => s.IsActive && s.UserId == request.UserId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var childIds = await _studentRepository.GetTableNoTracking()
                .Where(s => s.IsActive && s.GuardianId == guardian.Id)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            viewerStudentIds = viewerStudentIds.Union(childIds).Distinct().ToList();
        }

        var isInviteeOrGuardian = enrollmentRequest.GroupMembers.Any(gm =>
            gm.MemberType == GroupMemberType.Invited
            && viewerStudentIds.Contains(gm.StudentId));

        if (!isOwner && !isInviteeOrGuardian)
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.EnrollmentRequestId == enrollmentRequest.Id, cancellationToken);

        var isGroup = string.Equals(
            enrollmentRequest.Course.SessionType?.Code,
            "group",
            StringComparison.OrdinalIgnoreCase);

        var hasPendingInvites = enrollmentRequest.GroupMembers.Any(gm =>
            gm.MemberType == GroupMemberType.Invited
            && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending);

        var canRespondStage = enrollmentRequest.Status == RequestStatus.Pending
            || (enrollmentRequest.Status == RequestStatus.Approved
                && !enrollmentRequest.Course.IsFlexible
                && enrollment == null);

        var actionable = enrollmentRequest.GroupMembers
            .Where(gm => gm.MemberType == GroupMemberType.Invited
                      && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending
                      && viewerStudentIds.Contains(gm.StudentId)
                      && canRespondStage)
            .Select(gm => gm.StudentId)
            .Distinct()
            .ToList();

        var dto = _mapper.Map<EnrollmentRequestDetailDto>(enrollmentRequest);
        dto.ProposedScheduleDates = await ComputeProposedDatesAsync(enrollmentRequest, cancellationToken);
        dto.IsOwner = isOwner;
        dto.Kind = enrollment?.Kind
                   ?? (isGroup ? EnrollmentKind.Group : EnrollmentKind.Individual);
        dto.ViewerStudentIds = viewerStudentIds;
        dto.ActionableMemberStudentIds = actionable;
        dto.CanCancelInvite = isOwner
            && enrollment == null
            && hasPendingInvites
            && (enrollmentRequest.Status == RequestStatus.Pending
                || enrollmentRequest.Status == RequestStatus.Approved);
        dto.CanCancel = isOwner
            && (enrollmentRequest.Status == RequestStatus.Pending
                || enrollmentRequest.Status == RequestStatus.Approved)
            && (enrollment == null
                || enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment);
        dto.EnrollmentId = enrollment?.Id;
        dto.EnrollmentStatus = enrollment?.EnrollmentStatus;
        dto.AmountDue = enrollment?.AmountDue > 0
            ? enrollment.AmountDue
            : enrollmentRequest.EstimatedTotalPrice;
        dto.PaymentDeadline = enrollment?.PaymentDeadline;
        dto.PayParticipantId = enrollment?.Participants
            .OrderBy(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefault();
        dto.CanPay = isOwner
            && enrollment != null
            && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
            && !enrollment.PaidByUserId.HasValue
            && (!enrollment.PaymentDeadline.HasValue
                || enrollment.PaymentDeadline.Value >= DateTime.UtcNow);

        return Success(entity: dto);
    }

    private async Task<List<ProposedScheduleSlotDto>> ComputeProposedDatesAsync(
        Qalam.Data.Entity.Course.CourseEnrollmentRequest enrollmentRequest,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStart = enrollmentRequest.PreferredStartDate < today ? today : enrollmentRequest.PreferredStartDate;

        var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
            enrollmentRequest.Course.TeacherId,
            effectiveStart,
            enrollmentRequest.PreferredEndDate);

        if (enrollmentRequest.SelectedSessionSlots != null && enrollmentRequest.SelectedSessionSlots.Count > 0)
        {
            var ordered = enrollmentRequest.SelectedSessionSlots.OrderBy(x => x.SessionNumber).ToList();
            var selections = ordered.Select(x => (x.SessionDate, x.TeacherAvailabilityId)).ToList();
            var availabilityById = new Dictionary<int, Qalam.Data.Entity.Teacher.TeacherAvailability>();
            foreach (var row in ordered)
            {
                if (row.TeacherAvailability != null)
                    availabilityById[row.TeacherAvailabilityId] = row.TeacherAvailability;
            }
            foreach (var sa in enrollmentRequest.SelectedAvailabilities)
            {
                if (sa.TeacherAvailability != null)
                    availabilityById[sa.TeacherAvailability.Id] = sa.TeacherAvailability;
            }

            var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
                effectiveStart,
                enrollmentRequest.PreferredEndDate,
                availabilityById.Keys.ToList(),
                ct);

            var preview = _scheduleGenerator.PreviewExplicit(
                enrollmentRequest.Course,
                enrollmentRequest,
                selections,
                availabilityById,
                blockedExceptions,
                existingScheduledSlots,
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

        var slots = enrollmentRequest.SelectedAvailabilities
            .Select(sa => sa.TeacherAvailability)
            .Where(ta => ta != null)
            .ToList();

        if (slots.Count == 0)
            return new List<ProposedScheduleSlotDto>();

        var existingSlots = await _scheduleRepository.GetScheduledSlotsAsync(
            effectiveStart,
            enrollmentRequest.PreferredEndDate,
            slots.Select(s => s.Id).ToList(),
            ct);

        var roundRobinPreview = _scheduleGenerator.Preview(
            enrollmentRequest.Course,
            enrollmentRequest,
            slots,
            blockedExceptions,
            existingSlots,
            effectiveStart,
            enrollmentRequest.PreferredEndDate);

        return roundRobinPreview.Slots
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
