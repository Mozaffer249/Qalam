using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollmentParticipant;

/// <summary>
/// Mock / free-trial pay path: creates a succeeded payment then activates via
/// <see cref="IPaymentConfirmationService"/>. Card payments use CreatePaymentIntent + Confirm.
/// </summary>
public class PayEnrollmentParticipantCommandHandler : ResponseHandler,
    IRequestHandler<PayEnrollmentParticipantCommand, Response<PaymentResultDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStudentCoursePriceResolver _coursePriceResolver;
    private readonly IPaymentConfirmationService _confirmationService;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly PaymentSettings _settings;

    public PayEnrollmentParticipantCommandHandler(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IStudentCoursePriceResolver coursePriceResolver,
        IPaymentConfirmationService confirmationService,
        IPaymentGatewayResolver gatewayResolver,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _coursePriceResolver = coursePriceResolver;
        _confirmationService = confirmationService;
        _gatewayResolver = gatewayResolver;
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

        var ownerUserId = enrollment.EnrollmentRequest?.RequestedByUserId ?? enrollment.OwnerUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<PaymentResultDto>("Only the enrollment owner can pay for this enrollment.");

        if (enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
            return BadRequest<PaymentResultDto>("This enrollment has already been paid.");

        var totalAmount = _coursePriceResolver.ResolveEnrollmentPayableAmount(enrollment);

        var isFreeTrial = totalAmount == 0
            && enrollment.Source == EnrollmentSource.SessionRequest
            && enrollment.Kind == EnrollmentKind.Individual;

        if (totalAmount < 0 || (!isFreeTrial && totalAmount <= 0))
            return BadRequest<PaymentResultDto>("Enrollment amount due must be greater than zero.");

        if (isFreeTrial)
            totalAmount = 0;

        // Card path must use intent + confirm when a non-Mock gateway is active and amount > 0.
        if (totalAmount > 0)
        {
            try
            {
                var active = await _gatewayResolver.ResolveActiveAsync(cancellationToken);
                if (!active.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest<PaymentResultDto>(
                        "Use payment intent checkout for card payments.");
                }
            }
            catch (InvalidOperationException)
            {
                // Unconfigured active provider — fall through to mock only if amount is free-trial.
            }
        }

        var pendingParticipants = enrollment.Participants
            .Where(p => p.PaymentStatus == PaymentStatus.Pending)
            .ToList();

        if (pendingParticipants.Count == 0)
            return BadRequest<PaymentResultDto>("Enrollment has no payable participants.");

        var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
            || enrollment.CourseId == null;

        var payment = new Payment
        {
            PayerUserId = request.UserId,
            Currency = _settings.DefaultCurrency,
            PaymentProvider = _settings.MockProviderName,
            ProviderTransactionId = "MOCK-" + Guid.NewGuid().ToString("N")[..16],
            Subtotal = totalAmount,
            VatAmount = 0,
            DiscountAmount = 0,
            TotalAmount = totalAmount,
            Status = PaymentStatus.Succeeded
        };

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

        var outcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
        if (!outcome.Succeeded)
        {
            if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
                return BadRequest<PaymentResultDto>(outcome.ErrorCode);
            return BadRequest<PaymentResultDto>(outcome.ErrorMessage ?? outcome.ErrorCode ?? "Payment confirmation failed.");
        }

        return Success(entity: outcome.Result!);
    }
}
