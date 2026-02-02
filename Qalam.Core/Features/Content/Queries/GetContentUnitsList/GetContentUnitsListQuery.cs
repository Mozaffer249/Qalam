using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Content.Queries.GetContentUnitsList;

public class GetContentUnitsListQuery : IRequest<Response<PaginatedResult<ContentUnit>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? SubjectId { get; set; }
    public List<int>? TermIds { get; set; }  // Changed from TermId to support multiple term selection
    public string? UnitTypeCode { get; set; }  // For Quran filtering (QuranSurah/QuranPart) and other unit types
    public string? Search { get; set; }
}
