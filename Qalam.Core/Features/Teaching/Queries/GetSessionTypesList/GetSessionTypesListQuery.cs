using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;

public class GetSessionTypesListQuery : IRequest<Response<PaginatedResult<SessionType>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
