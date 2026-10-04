using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.SubmitAdminPayoutBatchForReview;

public class SubmitAdminPayoutBatchForReviewCommandHandler : ResponseHandler,
    IRequestHandler<SubmitAdminPayoutBatchForReviewCommand, Response<AdminPayoutBatchDto>>
{
    private readonly IPayoutService _payouts;

    public SubmitAdminPayoutBatchForReviewCommandHandler(
        IPayoutService payouts,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _payouts = payouts;
    }

    public async Task<Response<AdminPayoutBatchDto>> Handle(
        SubmitAdminPayoutBatchForReviewCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var batch = await _payouts.SubmitForReviewAsync(request.Id, request.ReviewedByUserId, cancellationToken);
            if (batch == null)
                return NotFound<AdminPayoutBatchDto>("Payout batch not found.");

            return Success(entity: batch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AdminPayoutBatchDto>(ex.Message);
        }
    }
}
