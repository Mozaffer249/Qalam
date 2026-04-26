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

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollment;

public class PayEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<PayEnrollmentCommand, Response<PaymentResultDto>>
{
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICourseEnrollmentPaymentRepository _enrollmentPaymentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly PaymentSettings _settings;

    public PayEnrollmentCommandHandler(
        ICourseEnrollmentRepository enrollmentRepository,
        IPaymentRepository paymentRepository,
        ICourseEnrollmentPaymentRepository enrollmentPaymentRepository,
        IGuardianRepository guardianRepository,
        IScheduleGenerationService scheduleGenerator,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentRepository = enrollmentRepository;
        _paymentRepository = paymentRepository;
        _enrollmentPaymentRepository = enrollmentPaymentRepository;
        _guardianRepository = guardianRepository;
        _scheduleGenerator = scheduleGenerator;
        _settings = settings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        PayEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var enrollmentId = request.Data.EnrollmentId;

        var enrollment = await _enrollmentRepository.GetByIdForPaymentAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            return NotFound<PaymentResultDto>("Enrollment not found.");

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return BadRequest<PaymentResultDto>("Only pending-payment enrollments can be paid.");

        var now = DateTime.UtcNow;
        if (enrollment.PaymentDeadline.HasValue && enrollment.PaymentDeadline.Value < now)
            return BadRequest<PaymentResultDto>("Payment deadline has expired.");

        if (enrollment.EnrollmentRequest == null)
            return BadRequest<PaymentResultDto>("Enrollment is missing its originating request — cannot generate schedules.");

        // Authorization: minor → guardian only; adult → student themselves only.
        var student = enrollment.Student;
        if (student.IsMinor)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            var isGuardian = student.GuardianId.HasValue
                          && guardian != null
                          && student.GuardianId.Value == guardian.Id;
            if (!isGuardian)
                return BadRequest<PaymentResultDto>("Only the guardian can pay for a minor student.");
        }
        else
        {
            if (student.UserId != request.UserId)
                return BadRequest<PaymentResultDto>("Only the student can pay for this enrollment.");
        }

        var amount = enrollment.EnrollmentRequest.EstimatedTotalPrice;
        if (amount <= 0)
            return BadRequest<PaymentResultDto>("Enrollment amount must be greater than zero.");

        var transaction = await _enrollmentRepository.BeginTransactionAsync();
        try
        {
            var payment = new Payment
            {
                PayerUserId = request.UserId,
                Currency = _settings.DefaultCurrency,
                PaymentProvider = _settings.MockProviderName,
                ProviderTransactionId = "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                Subtotal = amount,
                VatAmount = 0,
                DiscountAmount = 0,
                TotalAmount = amount,
                Status = PaymentStatus.Succeeded
            };
            payment.PaymentItems.Add(new PaymentItem
            {
                ItemType = PaymentItemType.CourseEnrollment,
                ReferenceId = enrollment.Id,
                Description = enrollment.Course?.Title,
                Amount = amount
            });
            await _paymentRepository.AddAsync(payment);

            await _enrollmentPaymentRepository.AddAsync(new CourseEnrollmentPayment
            {
                CourseEnrollmentId = enrollment.Id,
                PaymentId = payment.Id,
                Status = PaymentStatus.Succeeded
            });

            enrollment.EnrollmentStatus = EnrollmentStatus.Active;
            enrollment.ActivatedAt = now;

            var schedules = _scheduleGenerator.Generate(
                enrollment.Course!,
                enrollment.EnrollmentRequest!,
                courseEnrollmentId: enrollment.Id,
                courseGroupEnrollmentId: null,
                startDate: DateOnly.FromDateTime(now));

            foreach (var s in schedules)
                enrollment.CourseSchedules.Add(s);

            await _enrollmentRepository.SaveChangesAsync();
            await _enrollmentRepository.CommitAsync();

            return Success(entity: new PaymentResultDto
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                TotalAmount = payment.TotalAmount,
                Currency = payment.Currency,
                PaidAt = now,
                EnrollmentActivated = true,
                SchedulesCreated = schedules.Count
            });
        }
        catch
        {
            await _enrollmentRepository.RollBackAsync();
            throw;
        }
    }
}
