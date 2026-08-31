using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetPartialDomainActivationCandidates;

public class GetPartialDomainActivationCandidatesQuery : IRequest<Response<List<PartialDomainActivationCandidateDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
