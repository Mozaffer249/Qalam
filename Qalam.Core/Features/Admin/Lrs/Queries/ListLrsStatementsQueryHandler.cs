using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class ListLrsStatementsQueryHandler : ResponseHandler,
    IRequestHandler<ListLrsStatementsQuery, Response<PaginatedResult<AdminLrsStatementListItemDto>>>
{
    private readonly ILrsStatementLogRepository _repo;

    public ListLrsStatementsQueryHandler(
        ILrsStatementLogRepository repo,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<PaginatedResult<AdminLrsStatementListItemDto>>> Handle(
        ListLrsStatementsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repo.GetPagedAsync(request, cancellationToken);

        return Success(entity: new PaginatedResult<AdminLrsStatementListItemDto>
        {
            Items = items,
            Total = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }
}
