using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Quran.Queries.GetQuranLevelsList;

public class GetQuranLevelsListQuery : IRequest<Response<PaginatedResult<QuranLevel>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
