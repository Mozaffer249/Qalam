using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Common.Queries.GetActiveNationalities;

public class GetActiveNationalitiesQuery : IRequest<Response<List<NationalityPublicDto>>>
{
}
