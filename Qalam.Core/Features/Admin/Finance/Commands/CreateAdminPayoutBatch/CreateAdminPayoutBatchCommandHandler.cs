using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.CreateAdminPayoutBatch;

public class CreateAdminPayoutBatchCommandHandler : ResponseHandler,
    IRequestHandler<CreateAdminPayoutBatchCommand, Response<AdminPayoutBatchDto>>
{
    private readonly IPayoutService _payouts;

    public CreateAdminPayoutBatchCommandHandler(
        IPayoutService payouts,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _payouts = payouts;
    }

    public async Task<Response<AdminPayoutBatchDto>> Handle(
        CreateAdminPayoutBatchCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var batch = await _payouts.CreateBatchFromPendingAsync(
                request.Body?.PeriodStart,
                request.Body?.PeriodEnd,
                request.CreatedByUserId,
                cancellationToken);
            return Success(entity: batch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AdminPayoutBatchDto>(ex.Message);
        }
    }
}
