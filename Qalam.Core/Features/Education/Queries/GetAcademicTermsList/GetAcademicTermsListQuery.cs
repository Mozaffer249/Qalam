using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetAcademicTermsList;

public class GetAcademicTermsListQuery : IRequest<Response<List<AcademicTermDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? CurriculumId { get; set; }
}
