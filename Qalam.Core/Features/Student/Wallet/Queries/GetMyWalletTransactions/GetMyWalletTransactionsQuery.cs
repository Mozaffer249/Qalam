using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Wallet;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Wallet.Queries.GetMyWalletTransactions;

public class GetMyWalletTransactionsQuery : IRequest<Response<List<WalletTransactionDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>All (default), Added, Spent, Refunded, or an exact transaction type.</summary>
    public string? Type { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class GetMyWalletTransactionsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyWalletTransactionsQuery, Response<List<WalletTransactionDto>>>
{
    private readonly IStudentWalletService _walletService;

    public GetMyWalletTransactionsQueryHandler(
        IStudentWalletService walletService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _walletService = walletService;
    }

    public async Task<Response<List<WalletTransactionDto>>> Handle(
        GetMyWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);
        var (items, total) = await _walletService.ListTransactionsAsync(
            request.UserId, request.Type, page, pageSize, cancellationToken);
        return Success(entity: items, Meta: BuildPaginationMeta(page, pageSize, total));
    }
}
