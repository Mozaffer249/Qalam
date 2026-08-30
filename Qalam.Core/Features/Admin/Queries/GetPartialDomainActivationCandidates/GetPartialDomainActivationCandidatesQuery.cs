using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetPartialDomainActivationCandidates;

public class GetPartialDomainActivationCandidatesQuery : IRequest<Response<List<PartialDomainActivationCandidateDto>>>;
