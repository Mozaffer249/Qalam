using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Wallet;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Wallet.Queries.GetMyWallet;

public class GetMyWalletQuery : IRequest<Response<WalletSummaryDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}

public class GetMyWalletQueryHandler : ResponseHandler,
    IRequestHandler<GetMyWalletQuery, Response<WalletSummaryDto>>
{
    private readonly IStudentWalletService _walletService;

    public GetMyWalletQueryHandler(
        IStudentWalletService walletService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _walletService = walletService;
    }

    public async Task<Response<WalletSummaryDto>> Handle(GetMyWalletQuery request, CancellationToken cancellationToken)
        => Success(entity: await _walletService.GetSummaryAsync(request.UserId, cancellationToken));
}
