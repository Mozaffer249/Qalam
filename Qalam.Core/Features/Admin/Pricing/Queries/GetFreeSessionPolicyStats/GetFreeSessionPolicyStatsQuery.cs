using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.GetFreeSessionPolicyStats;

public class GetFreeSessionPolicyStatsQuery : IRequest<Response<FreeSessionPolicyStatsDto>>
{
}
