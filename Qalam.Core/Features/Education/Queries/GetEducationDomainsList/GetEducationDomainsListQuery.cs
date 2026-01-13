using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainsList;

public class GetEducationDomainsListQuery : IRequest<Response<PaginatedResult<EducationDomainDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
