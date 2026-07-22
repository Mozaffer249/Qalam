using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Universities.Queries.GetUniversitiesList;

public class GetUniversitiesListQueryHandler : ResponseHandler,
    IRequestHandler<GetUniversitiesListQuery, Response<List<UniversityDto>>>
{
    private readonly IUniversityRepository _repo;

    public GetUniversitiesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IUniversityRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<List<UniversityDto>>> Handle(GetUniversitiesListQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetUniversitiesDtoAsync(request.ActiveOnly, cancellationToken);
        return Success(entity: list);
    }
}
