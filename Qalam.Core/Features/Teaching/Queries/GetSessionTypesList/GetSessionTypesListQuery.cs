using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;

public class GetSessionTypesListQuery : IRequest<Response<List<SessionType>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
