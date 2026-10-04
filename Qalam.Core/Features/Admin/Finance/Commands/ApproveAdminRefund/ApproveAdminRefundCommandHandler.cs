using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.ApproveAdminRefund;

public class ApproveAdminRefundCommandHandler : ResponseHandler,
    IRequestHandler<ApproveAdminRefundCommand, Response<AdminRefundDetailDto>>
{
    private readonly IRefundService _refunds;

    public ApproveAdminRefundCommandHandler(
        IRefundService refunds,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _refunds = refunds;
    }

    public async Task<Response<AdminRefundDetailDto>> Handle(
        ApproveAdminRefundCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await _refunds.ApproveRefundAsync(
                request.Id, request.ApprovedByUserId ?? 0, cancellationToken);
            var detail = await _refunds.GetByIdAsync(refund.Id, cancellationToken);
            return Success(entity: detail!);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AdminRefundDetailDto>(ex.Message);
        }
    }
}
