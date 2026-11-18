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

namespace Qalam.Core.Features.Student.Payments.Commands.CreateIapIntent;

public class CreateIapIntentCommandHandler : ResponseHandler,
    IRequestHandler<CreateIapIntentCommand, Response<IapIntentDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStudentCoursePriceResolver _coursePriceResolver;
    private readonly AppleIapSettings _iapSettings;

    public CreateIapIntentCommandHandler(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IStudentCoursePriceResolver coursePriceResolver,
        IOptions<PaymentSettings> paymentSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _coursePriceResolver = coursePriceResolver;
        _iapSettings = paymentSettings.Value.AppleIap;
    }

    public async Task<Response<IapIntentDto>> Handle(
        CreateIapIntentCommand request,
        CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.GetByIdForPaymentAsync(
            request.Data.ParticipantId, cancellationToken);

        if (participant == null)
            return NotFound<IapIntentDto>("Enrollment participant not found.");

        var enrollment = participant.Enrollment;

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return BadRequest<IapIntentDto>("Only pending-payment enrollments can be paid.");

        var now = DateTime.UtcNow;
        if (enrollment.PaymentDeadline.HasValue && enrollment.PaymentDeadline.Value < now)
            return BadRequest<IapIntentDto>("Payment deadline has expired.");

        var ownerUserId = enrollment.EnrollmentRequest?.RequestedByUserId ?? enrollment.OwnerUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<IapIntentDto>("Only the enrollment owner can pay.");

        if (enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
            return BadRequest<IapIntentDto>("This enrollment has already been paid.");

        var totalAmount = _coursePriceResolver.ResolveEnrollmentPayableAmount(enrollment);
        if (totalAmount <= 0)
            return BadRequest<IapIntentDto>("Use the free-trial endpoint for zero-amount enrollments.");

        var tier = ResolveTier(totalAmount);
        if (tier == null)
            return BadRequest<IapIntentDto>("No IAP tier covers this amount.");

        var productId = $"{_iapSettings.ProductIdPrefix}{tier}";
        var transactionToken = Guid.NewGuid().ToString();

        var payment = new Payment
        {
            PayerUserId = request.UserId,
            PaymentProvider = AppleIapSettings.ProviderName,
            ProviderTransactionId = transactionToken,
            Status = PaymentStatus.Pending,
            Currency = "SAR",
            Subtotal = totalAmount,
            VatAmount = 0,
            DiscountAmount = 0,
            TotalAmount = totalAmount,
        };
        payment.PaymentItems.Add(new PaymentItem
        {
            ItemType = PaymentItemType.CourseEnrollment,
            ReferenceId = enrollment.Id,
            Amount = totalAmount,
            Description = $"IAP {productId}",
        });

        await _paymentRepository.AddAsync(payment);

        return Success(entity: new IapIntentDto
        {
            IapProductId = productId,
            TransactionToken = transactionToken,
            AmountSar = totalAmount,
        });
    }

    private int? ResolveTier(decimal amount)
    {
        var amountInt = (int)Math.Ceiling(amount);
        foreach (var tier in _iapSettings.Tiers.OrderBy(t => t))
        {
            if (tier >= amountInt) return tier;
        }
        return null;
    }
}
