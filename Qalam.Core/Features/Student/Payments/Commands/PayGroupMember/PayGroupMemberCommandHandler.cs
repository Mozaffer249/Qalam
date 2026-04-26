using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
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
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly PaymentSettings _settings;

    public PayGroupMemberCommandHandler(
        ICourseGroupEnrollmentRepository groupRepository,
        IPaymentRepository paymentRepository,
        IGroupEnrollmentMemberPaymentRepository memberPaymentRepository,
        IGuardianRepository guardianRepository,
        IScheduleGenerationService scheduleGenerator,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _groupRepository = groupRepository;
        _paymentRepository = paymentRepository;
        _memberPaymentRepository = memberPaymentRepository;
        _guardianRepository = guardianRepository;
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

                var schedules = _scheduleGenerator.Generate(
                    group.Course!,
                    group.EnrollmentRequest!,
                    courseEnrollmentId: null,
                    courseGroupEnrollmentId: group.Id,
                    startDate: DateOnly.FromDateTime(now));

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
