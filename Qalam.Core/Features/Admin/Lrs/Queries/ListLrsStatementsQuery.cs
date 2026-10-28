using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class ListLrsStatementsQuery : AdminLrsStatementFilter, IRequest<Response<PaginatedResult<AdminLrsStatementListItemDto>>>
{
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
