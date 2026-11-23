using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.CreateIapIntent;

public class CreateIapIntentCommandHandler : ResponseHandler,
    IRequestHandler<CreateIapIntentCommand, Response<IapIntentResponseDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepo;
    private readonly IStudentCoursePriceResolver _priceResolver;
    private readonly IAppleIapService _iap;

    public CreateIapIntentCommandHandler(
        IEnrollmentParticipantRepository participantRepo,
        IStudentCoursePriceResolver priceResolver,
        IAppleIapService iap,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepo = participantRepo;
        _priceResolver = priceResolver;
        _iap = iap;
    }

    public async Task<Response<IapIntentResponseDto>> Handle(
        CreateIapIntentCommand request,
        CancellationToken cancellationToken)
    {
        var participantId = request.Data.ParticipantId;

        var participant = await _participantRepo.GetByIdForPaymentAsync(participantId, cancellationToken);
        if (participant == null)
            return NotFound<IapIntentResponseDto>("Enrollment participant not found.");

        var enrollment = participant.Enrollment;

        // Ownership: only the enrollment payer may initiate IAP
        if (enrollment.OwnerUserId != request.UserId && enrollment.PaidByUserId != request.UserId)
            return BadRequest<IapIntentResponseDto>("Only the enrollment payer can initiate payment.");

        var amountSar = _priceResolver.ResolveEnrollmentPayableAmount(enrollment);
        if (amountSar <= 0)
        {
            // Free / already-paid — caller should use mock/wallet path instead
            return BadRequest<IapIntentResponseDto>("USE_MOCK_PAY");
        }

        var productId = _iap.ResolveProductId(amountSar);
        if (productId == null)
            return BadRequest<IapIntentResponseDto>(
                $"Amount {amountSar:F2} SAR exceeds the maximum IAP tier. Use card payment.");

        var token = _iap.CreateTransactionToken(participantId, request.UserId, productId, amountSar);

        return Success(entity: new IapIntentResponseDto
        {
            IapProductId = productId,
            TransactionToken = token,
            AmountSar = (double)amountSar
        });
    }
}
