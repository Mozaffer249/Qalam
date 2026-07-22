using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Universities.Queries.GetUniversitiesList;

public class GetUniversitiesListQuery : IRequest<Response<List<UniversityDto>>>
{
    public bool ActiveOnly { get; set; }
}
