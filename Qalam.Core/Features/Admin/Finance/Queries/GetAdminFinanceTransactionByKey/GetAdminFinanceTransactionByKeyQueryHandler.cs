using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceTransactionByKey;

public class GetAdminFinanceTransactionByKeyQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminFinanceTransactionByKeyQuery, Response<TeacherFinanceTransactionDetailDto>>
{
    private readonly IAdminFinanceTransactionService _transactions;

    public GetAdminFinanceTransactionByKeyQueryHandler(
        IAdminFinanceTransactionService transactions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _transactions = transactions;
    }

    public async Task<Response<TeacherFinanceTransactionDetailDto>> Handle(
        GetAdminFinanceTransactionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _transactions.GetTransactionDetailAsync(
            request.Key, cancellationToken);
        if (detail == null)
            return NotFound<TeacherFinanceTransactionDetailDto>("Transaction not found.");

        return Success(entity: detail);
    }
}
