using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Universities.Queries.GetUniversityById;

public class GetUniversityByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetUniversityByIdQuery, Response<UniversityDto>>
{
    private readonly IUniversityRepository _repo;

    public GetUniversityByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IUniversityRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<UniversityDto>> Handle(GetUniversityByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repo.GetUniversityDtoByIdAsync(request.Id, cancellationToken);
        return item is null ? NotFound<UniversityDto>("University not found") : Success(entity: item);
    }
}
