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

namespace Qalam.Core.Features.Student.Payments.Commands.PayGroupMember;

public class PayGroupMemberCommandHandler : ResponseHandler,
    IRequestHandler<PayGroupMemberCommand, Response<PaymentResultDto>>
{
    private readonly ICourseGroupEnrollmentRepository _groupRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IGroupEnrollmentMemberPaymentRepository _memberPaymentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly PaymentSettings _settings;

    public PayGroupMemberCommandHandler(
        ICourseGroupEnrollmentRepository groupRepository,
        IPaymentRepository paymentRepository,
        IGroupEnrollmentMemberPaymentRepository memberPaymentRepository,
        IGuardianRepository guardianRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _groupRepository = groupRepository;
        _paymentRepository = paymentRepository;
        _memberPaymentRepository = memberPaymentRepository;
        _guardianRepository = guardianRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _settings = settings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        PayGroupMemberCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;

        var group = await _groupRepository.GetByIdForPaymentAsync(dto.GroupEnrollmentId, cancellationToken);
        if (group == null)
            return NotFound<PaymentResultDto>("Group enrollment not found.");

        if (group.Status != EnrollmentStatus.PendingPayment)
            return BadRequest<PaymentResultDto>("Only pending-payment group enrollments can be paid.");

        var now = DateTime.UtcNow;
        if (group.PaymentDeadline.HasValue && group.PaymentDeadline.Value < now)
            return BadRequest<PaymentResultDto>("Payment deadline has expired.");

        if (group.EnrollmentRequest == null)
            return BadRequest<PaymentResultDto>("Group enrollment is missing its originating request — cannot generate schedules.");

        var member = group.Members.FirstOrDefault(m => m.StudentId == dto.StudentId);
        if (member == null)
            return NotFound<PaymentResultDto>("Member not found in this group enrollment.");

        if (member.PaymentStatus == PaymentStatus.Succeeded)
            return BadRequest<PaymentResultDto>("This member has already paid.");

        if (member.PaymentStatus == PaymentStatus.Cancelled)
            return BadRequest<PaymentResultDto>("This member's payment was cancelled.");

        // Authorization on the target student (NOT the leader / request owner).
        var targetStudent = member.Student;
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
                return BadRequest<PaymentResultDto>("Only the student themselves can pay for this membership.");
        }

        // Compute the per-member share. The last payer absorbs any rounding remainder
        // so that the sum of all member payments equals EstimatedTotalPrice exactly.
        var totalAmount = group.EnrollmentRequest.EstimatedTotalPrice;
        var memberCount = group.Members.Count;
        if (memberCount <= 0)
            return BadRequest<PaymentResultDto>("Group enrollment has no members.");

        var baseShare = Math.Round(totalAmount / memberCount, 2, MidpointRounding.AwayFromZero);
        var succeededCount = group.Members.Count(m => m.PaymentStatus == PaymentStatus.Succeeded);
        var pendingCount = group.Members.Count(m => m.PaymentStatus == PaymentStatus.Pending);
        var isLastPending = pendingCount == 1;

        var share = isLastPending
            ? totalAmount - (baseShare * succeededCount)
            : baseShare;

        if (share <= 0)
            return BadRequest<PaymentResultDto>("Computed member share must be greater than zero.");

        var transaction = await _groupRepository.BeginTransactionAsync();
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
                ReferenceId = group.Id,
                Description = group.Course?.Title,
                Amount = share
            });
            await _paymentRepository.AddAsync(payment);

            await _memberPaymentRepository.AddAsync(new GroupEnrollmentMemberPayment
            {
                CourseGroupEnrollmentMemberId = member.Id,
                PaymentId = payment.Id,
                Status = PaymentStatus.Succeeded
            });

            member.PaymentStatus = PaymentStatus.Succeeded;
            member.PaidAt = now;

            var allPaid = group.Members.All(m => m.PaymentStatus == PaymentStatus.Succeeded);
            var schedulesCreated = 0;

            if (allPaid)
            {
                group.Status = EnrollmentStatus.Active;
                group.ActivatedAt = now;

                // Compute effective start; never go into the past.
                var today = DateOnly.FromDateTime(now);
                var effectiveStart = group.EnrollmentRequest!.PreferredStartDate < today
                    ? today
                    : group.EnrollmentRequest.PreferredStartDate;

                // Race-loser check: re-validate against existing CourseSchedules right
                // before we persist (mock provider; rollback is clean).
                var enrollmentRequest = group.EnrollmentRequest!;

                var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
                    group.Course!.TeacherId,
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
                        group.Course!,
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
                        group.Course!,
                        enrollmentRequest,
                        slots,
                        blockedExceptions,
                        existingScheduledSlots,
                        effectiveStart,
                        enrollmentRequest.PreferredEndDate);
                }

                if (preview.Conflicts.Count > 0)
                {
                    await _groupRepository.RollBackAsync();
                    return BadRequest<PaymentResultDto>(
                        "Some of your scheduled dates were just booked by another student. Please re-submit with different dates.");
                }

                if (!preview.FitsInWindow)
                {
                    await _groupRepository.RollBackAsync();
                    return BadRequest<PaymentResultDto>(
                        $"Schedule no longer fits before {group.EnrollmentRequest.PreferredEndDate:yyyy-MM-dd}. Please re-submit with a longer window.");
                }

                var schedules = preview.Slots
                    .Select(s => new Qalam.Data.Entity.Course.CourseSchedule
                    {
                        Date = s.Date,
                        TeacherAvailabilityId = s.TeacherAvailabilityId,
                        DurationMinutes = s.DurationMinutes,
                        TeachingModeId = group.Course!.TeachingModeId,
                        LocationId = null,
                        Status = ScheduleStatus.Scheduled
                    })
                    .ToList();

                foreach (var s in schedules)
                    group.CourseSchedules.Add(s);

                schedulesCreated = schedules.Count;
            }

            await _groupRepository.SaveChangesAsync();
            await _groupRepository.CommitAsync();

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
            await _groupRepository.RollBackAsync();
            throw;
        }
    }
}
