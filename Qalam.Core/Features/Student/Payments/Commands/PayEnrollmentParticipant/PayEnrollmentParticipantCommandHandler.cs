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

public class PayEnrollmentParticipantCommandHandler : ResponseHandler,
    IRequestHandler<PayEnrollmentParticipantCommand, Response<PaymentResultDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEnrollmentPaymentRepository _enrollmentPaymentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly PaymentSettings _settings;

    public PayEnrollmentParticipantCommandHandler(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IEnrollmentPaymentRepository enrollmentPaymentRepository,
        IGuardianRepository guardianRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _enrollmentPaymentRepository = enrollmentPaymentRepository;
        _guardianRepository = guardianRepository;
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

        if (enrollment.EnrollmentRequest == null)
            return BadRequest<PaymentResultDto>("Enrollment is missing its originating request — cannot generate schedules.");

        if (participant.PaymentStatus == PaymentStatus.Succeeded)
            return BadRequest<PaymentResultDto>("This participant has already paid.");

        if (participant.PaymentStatus == PaymentStatus.Cancelled)
            return BadRequest<PaymentResultDto>("This participant's payment was cancelled.");

        // Authorization on the target student.
        var targetStudent = participant.Student;
        if (targetStudent == null)
            return BadRequest<PaymentResultDto>("Target student record is unavailable.");

        if (targetStudent.IsMinor)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            var isGuardian = targetStudent.GuardianId.HasValue
                          && guardian != null
                          && targetStudent.GuardianId.Value == guardian.Id;
            if (!isGuardian)
                return BadRequest<PaymentResultDto>("Only the guardian can pay for a minor student.");
        }
        else
        {
            if (targetStudent.UserId != request.UserId)
                return BadRequest<PaymentResultDto>("Only the student themselves can pay for this participation.");
        }

        // Compute this participant's share. Individual: full amount. Group: equal split, with
        // the LAST pending payer absorbing rounding remainder so the sum equals total exactly.
        var totalAmount = enrollment.EnrollmentRequest.EstimatedTotalPrice;
        var participantCount = enrollment.Participants.Count;
        if (participantCount <= 0)
            return BadRequest<PaymentResultDto>("Enrollment has no participants.");

        decimal share;
        if (enrollment.Kind == EnrollmentKind.Individual)
        {
            share = totalAmount;
        }
        else
        {
            var baseShare = Math.Round(totalAmount / participantCount, 2, MidpointRounding.AwayFromZero);
            var succeededCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded);
            var pendingCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Pending);
            var isLastPending = pendingCount == 1;

            share = isLastPending
                ? totalAmount - (baseShare * succeededCount)
                : baseShare;
        }

        if (share <= 0)
            return BadRequest<PaymentResultDto>("Computed participant share must be greater than zero.");

        var transaction = await _participantRepository.BeginTransactionAsync();
        try
        {
            var payment = new Payment
            {
                PayerUserId = request.UserId,
                Currency = _settings.DefaultCurrency,
                PaymentProvider = _settings.MockProviderName,
                ProviderTransactionId = "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                Subtotal = share,
                VatAmount = 0,
                DiscountAmount = 0,
                TotalAmount = share,
                Status = PaymentStatus.Succeeded
            };
            payment.PaymentItems.Add(new PaymentItem
            {
                ItemType = PaymentItemType.CourseEnrollment,
                ReferenceId = enrollment.Id,
                Description = enrollment.Course?.Title,
                Amount = share
            });
            await _paymentRepository.AddAsync(payment);

            await _enrollmentPaymentRepository.AddAsync(new EnrollmentPayment
            {
                EnrollmentParticipantId = participant.Id,
                PaymentId = payment.Id,
                Status = PaymentStatus.Succeeded
            });

            participant.PaymentStatus = PaymentStatus.Succeeded;
            participant.PaidAt = now;

            var allPaid = enrollment.Participants.All(p => p.PaymentStatus == PaymentStatus.Succeeded);
            var schedulesCreated = 0;

            if (allPaid)
            {
                enrollment.EnrollmentStatus = EnrollmentStatus.Active;
                enrollment.ActivatedAt = now;

                var today = DateOnly.FromDateTime(now);
                var enrollmentRequest = enrollment.EnrollmentRequest!;
                var effectiveStart = enrollmentRequest.PreferredStartDate < today
                    ? today
                    : enrollmentRequest.PreferredStartDate;

                // Race-loser re-check: if a competing student paid for the same slot between
                // our submit and now, reject before persisting (mock provider — clean rollback).
                var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
                    enrollment.Course!.TeacherId,
                    effectiveStart,
                    enrollmentRequest.PreferredEndDate);

                ScheduleGenerationResult preview;

                if (enrollmentRequest.SelectedSessionSlots != null && enrollmentRequest.SelectedSessionSlots.Count > 0)
                {
                    var ordered = enrollmentRequest.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
                    var selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
                    var availabilityById = new Dictionary<int, TeacherAvailability>();
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
                        cancellationToken);

                    preview = _scheduleGenerator.PreviewExplicit(
                        enrollment.Course!,
                        enrollmentRequest,
                        selections,
                        availabilityById,
                        blockedExceptions,
                        existingScheduledSlots,
                        enrollmentRequest.PreferredEndDate);
                }
                else
                {
                    var slots = enrollmentRequest.SelectedAvailabilities
                        .Select(sa => sa.TeacherAvailability)
                        .Where(ta => ta != null)
                        .ToList();

                    var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
                        effectiveStart,
                        enrollmentRequest.PreferredEndDate,
                        slots.Select(s => s.Id).ToList(),
                        cancellationToken);

                    preview = _scheduleGenerator.Preview(
                        enrollment.Course!,
                        enrollmentRequest,
                        slots,
                        blockedExceptions,
                        existingScheduledSlots,
                        effectiveStart,
                        enrollmentRequest.PreferredEndDate);
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
                        $"Schedule no longer fits before {enrollmentRequest.PreferredEndDate:yyyy-MM-dd}. Please re-submit with a longer window.");
                }

                foreach (var s in preview.Slots)
                {
                    enrollment.CourseSchedules.Add(new CourseSchedule
                    {
                        Date = s.Date,
                        TeacherAvailabilityId = s.TeacherAvailabilityId,
                        DurationMinutes = s.DurationMinutes,
                        TeachingModeId = enrollment.Course!.TeachingModeId,
                        LocationId = null,
                        Status = ScheduleStatus.Scheduled
                    });
                }

                schedulesCreated = preview.Slots.Count;
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
                EnrollmentActivated = allPaid,
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
