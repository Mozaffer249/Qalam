using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.Queries.GetPartialDomainActivationCandidateIds;

public class GetPartialDomainActivationCandidateIdsQuery : IRequest<Response<List<int>>>;
