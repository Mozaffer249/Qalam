using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Quran;

namespace Qalam.Core.Features.Quran.Queries.GetQuranContentTypesList;

public class GetQuranContentTypesListQuery : IRequest<Response<List<QuranContentType>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
}
