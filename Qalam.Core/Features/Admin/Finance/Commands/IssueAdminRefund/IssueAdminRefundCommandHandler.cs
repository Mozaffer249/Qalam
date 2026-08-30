using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.IssueAdminRefund;

public class IssueAdminRefundCommandHandler : ResponseHandler,
    IRequestHandler<IssueAdminRefundCommand, Response<AdminRefundDetailDto>>
{
    private readonly IRefundService _refunds;

    public IssueAdminRefundCommandHandler(
        IRefundService refunds,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _refunds = refunds;
    }

    public async Task<Response<AdminRefundDetailDto>> Handle(
        IssueAdminRefundCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Body;
        try
        {
            if (body.PaymentId.HasValue && body.EnrollmentId.HasValue && body.Amount.HasValue)
            {
                var refund = await _refunds.IssueRefundAsync(
                    body.PaymentId.Value,
                    body.EnrollmentId.Value,
                    body.Amount.Value,
                    "SAR",
                    body.Reason,
                    request.InitiatedByUserId,
                    cancellationToken);
                var detail = await _refunds.GetByIdAsync(refund.Id, cancellationToken);
                return Success(entity: detail!);
            }

            if (body.EnrollmentId.HasValue)
            {
                var refunds = await _refunds.RefundEnrollmentPaymentsAsync(
                    body.EnrollmentId.Value,
                    string.IsNullOrWhiteSpace(body.Reason) ? "Admin refund" : body.Reason,
                    request.InitiatedByUserId,
                    cancellationToken);
                if (refunds.Count == 0)
                    return BadRequest<AdminRefundDetailDto>("No refundable payments for this enrollment.");

                var detail = await _refunds.GetByIdAsync(refunds[0].Id, cancellationToken);
                return Success(entity: detail!);
            }

            return BadRequest<AdminRefundDetailDto>(
                "Provide PaymentId + EnrollmentId + Amount, or EnrollmentId for full refund.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AdminRefundDetailDto>(ex.Message);
        }
    }
}
