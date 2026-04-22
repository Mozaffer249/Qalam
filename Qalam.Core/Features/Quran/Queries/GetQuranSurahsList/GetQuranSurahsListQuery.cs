using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Quran;

namespace Qalam.Core.Features.Quran.Queries.GetQuranSurahsList;

public class GetQuranSurahsListQuery : IRequest<Response<List<QuranSurah>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int? PartNumber { get; set; }
    public string? Search { get; set; }
}
