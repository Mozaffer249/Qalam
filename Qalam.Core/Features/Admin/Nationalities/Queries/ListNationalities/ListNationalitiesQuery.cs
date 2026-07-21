using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Admin.Nationalities.Queries.ListNationalities;

public class ListNationalitiesQuery : IRequest<Response<List<NationalityAdminDto>>>
{
}
