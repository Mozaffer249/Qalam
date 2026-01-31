using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetFilterOptions;

public class GetFilterOptionsQuery : IRequest<Response<FilterOptionsResponseDto>>
{
    public int DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public int? SubjectId { get; set; }
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
    public string? UnitTypeCode { get; set; }
    
    // Pagination parameters
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
