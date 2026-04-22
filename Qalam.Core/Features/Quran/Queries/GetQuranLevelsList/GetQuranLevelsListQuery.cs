using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Quran;

namespace Qalam.Core.Features.Quran.Queries.GetQuranLevelsList;

public class GetQuranLevelsListQuery : IRequest<Response<List<QuranLevel>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
