using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetFilterOptions;

public class GetFilterOptionsQuery : IRequest<Response<FilterOptionsResponseDto>>
{
    public string DomainCode { get; set; } = default!;
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public int? SubjectId { get; set; }
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
}
