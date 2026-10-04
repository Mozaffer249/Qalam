using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.RejectAdminRefund;

public class RejectAdminRefundCommandHandler : ResponseHandler,
    IRequestHandler<RejectAdminRefundCommand, Response<AdminRefundDetailDto>>
{
    private readonly IRefundService _refunds;

    public RejectAdminRefundCommandHandler(
        IRefundService refunds,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _refunds = refunds;
    }

    public async Task<Response<AdminRefundDetailDto>> Handle(
        RejectAdminRefundCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await _refunds.RejectRefundAsync(
                request.Id, request.RejectedByUserId ?? 0, request.Reason, cancellationToken);
            var detail = await _refunds.GetByIdAsync(refund.Id, cancellationToken);
            return Success(entity: detail!);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AdminRefundDetailDto>(ex.Message);
        }
    }
}
