using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListTeacherLevelTiers;

public class ListTeacherLevelTiersQuery : IRequest<Response<List<TeacherLevelTierAdminDto>>>
{
}
