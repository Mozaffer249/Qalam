using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollmentParticipant;

/// <summary>
/// Single-payer: request owner pays full <see cref="Enrollment.AmountDue"/> once;
/// all participants become Succeeded and enrollment activates.
/// </summary>
public class PayEnrollmentParticipantCommandHandler : ResponseHandler,
    IRequestHandler<PayEnrollmentParticipantCommand, Response<PaymentResultDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEnrollmentPaymentRepository _enrollmentPaymentRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly PaymentSettings _settings;

    public PayEnrollmentParticipantCommandHandler(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IEnrollmentPaymentRepository enrollmentPaymentRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _enrollmentPaymentRepository = enrollmentPaymentRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _settings = settings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        PayEnrollmentParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var participantId = request.Data.ParticipantId;

        var participant = await _participantRepository.GetByIdForPaymentAsync(participantId, cancellationToken);
        if (participant == null)
            return NotFound<PaymentResultDto>("Enrollment participant not found.");

        var enrollment = participant.Enrollment;

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return BadRequest<PaymentResultDto>("Only pending-payment enrollments can be paid.");

        var now = DateTime.UtcNow;
        if (enrollment.PaymentDeadline.HasValue && enrollment.PaymentDeadline.Value < now)
            return BadRequest<PaymentResultDto>("Payment deadline has expired.");

        if (enrollment.EnrollmentRequest == null
            && (enrollment.SelectedSessionSlots == null || enrollment.SelectedSessionSlots.Count == 0))
            return BadRequest<PaymentResultDto>(
                "Enrollment is missing schedule selections — cannot generate schedules.");

        // Single payer: request owner when request-backed; otherwise Enrollment.OwnerUserId.
        var ownerUserId = enrollment.EnrollmentRequest?.RequestedByUserId ?? enrollment.OwnerUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<PaymentResultDto>("Only the enrollment owner can pay for this enrollment.");

        if (enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
            return BadRequest<PaymentResultDto>("This enrollment has already been paid.");

        var totalAmount = enrollment.AmountDue > 0
            ? enrollment.AmountDue
            : enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0;

        if (totalAmount <= 0)
            return BadRequest<PaymentResultDto>("Enrollment amount due must be greater than zero.");

        var pendingParticipants = enrollment.Participants
            .Where(p => p.PaymentStatus == PaymentStatus.Pending)
            .ToList();

        if (pendingParticipants.Count == 0)
            return BadRequest<PaymentResultDto>("Enrollment has no payable participants.");

        var transaction = await _participantRepository.BeginTransactionAsync();
        try
        {
            var payment = new Payment
            {
                PayerUserId = request.UserId,
                Currency = _settings.DefaultCurrency,
                PaymentProvider = _settings.MockProviderName,
                ProviderTransactionId = "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                Subtotal = totalAmount,
                VatAmount = 0,
                DiscountAmount = 0,
                TotalAmount = totalAmount,
                Status = PaymentStatus.Succeeded
            };
            var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
                || enrollment.CourseId == null;

            payment.PaymentItems.Add(new PaymentItem
            {
                ItemType = PaymentItemType.CourseEnrollment,
                ReferenceId = enrollment.Id,
                Description = isSessionRequest
                    ? (enrollment.OpenSessionRequest?.Subject?.NameEn
                       ?? enrollment.OpenSessionRequest?.Subject?.NameAr
                       ?? "Session request enrollment")
                    : enrollment.Course?.Title,
                Amount = totalAmount
            });
            await _paymentRepository.AddAsync(payment);

            foreach (var p in pendingParticipants)
            {
                await _enrollmentPaymentRepository.AddAsync(new EnrollmentPayment
                {
                    EnrollmentParticipantId = p.Id,
                    PaymentId = payment.Id,
                    Status = PaymentStatus.Succeeded
                });
                p.PaymentStatus = PaymentStatus.Succeeded;
                p.PaidAt = now;
            }

            enrollment.PaidByUserId = request.UserId;
            enrollment.AmountDue = totalAmount;

            var schedulesCreated = 0;
            enrollment.EnrollmentStatus = EnrollmentStatus.Active;
            enrollment.ActivatedAt = now;

            var today = DateOnly.FromDateTime(now);
            var enrollmentRequest = enrollment.EnrollmentRequest;
            DateOnly preferredStart;
            DateOnly preferredEnd;
            List<(DateOnly Date, int TeacherAvailabilityId)> selections;
            Dictionary<int, TeacherAvailability> availabilityById;

            if (enrollmentRequest != null
                && enrollmentRequest.SelectedSessionSlots != null
                && enrollmentRequest.SelectedSessionSlots.Count > 0)
            {
                preferredStart = enrollmentRequest.PreferredStartDate;
                preferredEnd = enrollmentRequest.PreferredEndDate;
                var ordered = enrollmentRequest.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
                selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
                availabilityById = new Dictionary<int, TeacherAvailability>();
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
            }
            else
            {
                preferredStart = enrollment.PreferredStartDate ?? today;
                preferredEnd = enrollment.PreferredEndDate ?? preferredStart.AddYears(2);
                var ordered = enrollment.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
                selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
                availabilityById = new Dictionary<int, TeacherAvailability>();
                foreach (var row in ordered)
                {
                    if (row.TeacherAvailability != null)
                        availabilityById[row.TeacherAvailabilityId] = row.TeacherAvailability;
                }
            }

            var effectiveStart = preferredStart < today ? today : preferredStart;
            var teacherId = isSessionRequest
                ? enrollment.ApprovedByTeacherId
                : enrollment.Course!.TeacherId;

            var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
                teacherId,
                effectiveStart,
                preferredEnd);

            var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
                effectiveStart,
                preferredEnd,
                availabilityById.Keys.ToList(),
                cancellationToken);

            ScheduleGenerationResult preview;
            int teachingModeId;
            Dictionary<int, int>? courseSessionIdByNumber = null;

            if (isSessionRequest)
            {
                if (selections.Count == 0)
                {
                    await _participantRepository.RollBackAsync();
                    return BadRequest<PaymentResultDto>(
                        "Enrollment has no selected session slots to schedule.");
                }

                teachingModeId = enrollment.OpenSessionRequest?.TeachingModeId
                    ?? throw new InvalidOperationException("Session request enrollment missing OpenSessionRequest.");

                var durationBySession = enrollment.OpenSessionRequest!.Sessions
                    .ToDictionary(s => s.SequenceNumber, s => s.DurationMinutes);

                var proposed = selections
                    .Select((sel, idx) =>
                    {
                        var sessionNumber = enrollment.SelectedSessionSlots
                            .OrderBy(s => s.SessionNumber)
                            .ElementAt(idx).SessionNumber;
                        var duration = durationBySession.TryGetValue(sessionNumber, out var d)
                            ? d
                            : (availabilityById.TryGetValue(sel.TeacherAvailabilityId, out var ta)
                               && ta.TimeSlot != null
                                ? ta.TimeSlot.ResolveDurationMinutes()
                                : 60);
                        return new CourseRequestProposedSession
                        {
                            SessionNumber = sessionNumber,
                            DurationMinutes = duration
                        };
                    })
                    .ToList();

                var stubCourse = new Course { IsFlexible = true, TeachingModeId = teachingModeId };
                var stubRequest = new CourseEnrollmentRequest { ProposedSessions = proposed };

                preview = _scheduleGenerator.PreviewExplicit(
                    stubCourse,
                    stubRequest,
                    selections,
                    availabilityById,
                    blockedExceptions,
                    existingScheduledSlots,
                    preferredEnd);
            }
            else
            {
                teachingModeId = enrollment.Course!.TeachingModeId;
                var stubOrRequest = enrollmentRequest ?? new CourseEnrollmentRequest { ProposedSessions = [] };

                if (selections.Count > 0)
                {
                    preview = _scheduleGenerator.PreviewExplicit(
                        enrollment.Course!,
                        stubOrRequest,
                        selections,
                        availabilityById,
                        blockedExceptions,
                        existingScheduledSlots,
                        preferredEnd);
                }
                else if (enrollmentRequest != null)
                {
                    var slots = enrollmentRequest.SelectedAvailabilities
                        .Select(sa => sa.TeacherAvailability)
                        .Where(ta => ta != null)
                        .ToList();

                    var existingForAvail = await _scheduleRepository.GetScheduledSlotsAsync(
                        effectiveStart,
                        preferredEnd,
                        slots.Select(s => s!.Id).ToList(),
                        cancellationToken);

                    preview = _scheduleGenerator.Preview(
                        enrollment.Course!,
                        enrollmentRequest,
                        slots!,
                        blockedExceptions,
                        existingForAvail,
                        effectiveStart,
                        preferredEnd);
                }
                else
                {
                    await _participantRepository.RollBackAsync();
                    return BadRequest<PaymentResultDto>(
                        "Enrollment has no selected session slots to schedule.");
                }

                if (!enrollment.Course!.IsFlexible && enrollment.Course.Sessions != null)
                {
                    courseSessionIdByNumber = enrollment.Course.Sessions
                        .GroupBy(cs => cs.SessionNumber)
                        .ToDictionary(g => g.Key, g => g.First().Id);
                }
            }

            if (preview.Conflicts.Count > 0)
            {
                await _participantRepository.RollBackAsync();
                return BadRequest<PaymentResultDto>(
                    "Some of your scheduled dates were just booked by another student. Please re-submit with different dates.");
            }

            if (!preview.FitsInWindow)
            {
                await _participantRepository.RollBackAsync();
                return BadRequest<PaymentResultDto>(
                    $"Schedule no longer fits before {preferredEnd:yyyy-MM-dd}. Please re-submit with a longer window.");
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
                    TeachingModeId = teachingModeId,
                    CourseSessionId = courseSessionId,
                    LocationId = null,
                    Status = ScheduleStatus.Scheduled
                });
            }

            schedulesCreated = preview.Slots.Count;

            if (isSessionRequest && enrollment.OpenSessionRequest != null)
            {
                enrollment.OpenSessionRequest.Status = OpenSessionRequestStatus.Paid;
                enrollment.OpenSessionRequest.UpdatedAt = now;
                await _participantRepository.SaveChangesAsync();

                enrollment.OpenSessionRequest.Status = OpenSessionRequestStatus.Scheduled;
                enrollment.OpenSessionRequest.UpdatedAt = DateTime.UtcNow;
            }

            await _participantRepository.SaveChangesAsync();
            await _participantRepository.CommitAsync();

            return Success(entity: new PaymentResultDto
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                TotalAmount = payment.TotalAmount,
                Currency = payment.Currency,
                PaidAt = now,
                EnrollmentActivated = true,
                SchedulesCreated = schedulesCreated
            });
        }
        catch
        {
            await _participantRepository.RollBackAsync();
            throw;
        }
    }
}
