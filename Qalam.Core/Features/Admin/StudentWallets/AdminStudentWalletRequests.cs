using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Wallet;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Admin.StudentWallets;

public class GetAdminStudentWalletQuery : IRequest<Response<AdminStudentWalletDto>>
{
    public int StudentId { get; set; }
}

public class GetAdminStudentWalletTransactionsQuery : IRequest<Response<List<WalletTransactionDto>>>
{
    public int StudentId { get; set; }
    public string? Type { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdjustStudentWalletCommand : IRequest<Response<AdminStudentWalletDto>>
{
    public int StudentId { get; set; }
    public int AdminUserId { get; set; }
    public AdminWalletAdjustmentDto Data { get; set; } = new();
}

public class AdjustStudentWalletCommandValidator : AbstractValidator<AdjustStudentWalletCommand>
{
    public AdjustStudentWalletCommandValidator()
    {
        RuleFor(x => x.Data.Amount).NotEqual(0);
        RuleFor(x => x.Data.Reason).NotEmpty().MaximumLength(300);
    }
}

public class AdminStudentWalletHandlers : ResponseHandler,
    IRequestHandler<GetAdminStudentWalletQuery, Response<AdminStudentWalletDto>>,
    IRequestHandler<GetAdminStudentWalletTransactionsQuery, Response<List<WalletTransactionDto>>>,
    IRequestHandler<AdjustStudentWalletCommand, Response<AdminStudentWalletDto>>
{
    private readonly IStudentWalletService _walletService;

    public AdminStudentWalletHandlers(
        IStudentWalletService walletService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _walletService = walletService;
    }

    public async Task<Response<AdminStudentWalletDto>> Handle(
        GetAdminStudentWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetForStudentAsync(request.StudentId, cancellationToken);
        return wallet == null
            ? NotFound<AdminStudentWalletDto>("Student not found.")
            : Success(entity: wallet);
    }

    public async Task<Response<List<WalletTransactionDto>>> Handle(
        GetAdminStudentWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetForStudentAsync(request.StudentId, cancellationToken);
        if (wallet == null)
            return NotFound<List<WalletTransactionDto>>("Student not found.");

        var page = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);
        var (items, total) = await _walletService.ListTransactionsAsync(
            wallet.PayerUserId, request.Type, page, pageSize, cancellationToken);
        return Success(entity: items, Meta: BuildPaginationMeta(page, pageSize, total));
    }

    public async Task<Response<AdminStudentWalletDto>> Handle(
        AdjustStudentWalletCommand request,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetForStudentAsync(request.StudentId, cancellationToken);
        if (wallet == null)
            return NotFound<AdminStudentWalletDto>("Student not found.");

        var amount = request.Data.Amount;
        var entry = new WalletEntryRequest
        {
            UserId = wallet.PayerUserId,
            Amount = Math.Abs(amount),
            Type = amount > 0 ? WalletTransactionType.AdminCredit : WalletTransactionType.AdminDebit,
            Description = request.Data.Reason.Trim(),
            ReasonCode = "ADMIN_ADJUSTMENT",
            CreatedByUserId = request.AdminUserId > 0 ? request.AdminUserId : null
        };

        var result = amount > 0
            ? await _walletService.CreditAsync(entry, cancellationToken)
            : await _walletService.DebitAsync(entry, cancellationToken);

        if (!result.Succeeded)
            return BadRequest<AdminStudentWalletDto>(result.ErrorCode ?? StudentWalletService.InvalidAmount);

        return Success(entity: (await _walletService.GetForStudentAsync(request.StudentId, cancellationToken))!);
    }
}
