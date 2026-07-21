using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Common.Queries.GetActiveNationalities;

public class GetActiveNationalitiesQueryHandler : ResponseHandler,
    IRequestHandler<GetActiveNationalitiesQuery, Response<List<NationalityPublicDto>>>
{
    private readonly INationalityProvider _provider;

    public GetActiveNationalitiesQueryHandler(
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<List<NationalityPublicDto>>> Handle(
        GetActiveNationalitiesQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = await _provider.GetActivePublicDtosAsync(cancellationToken);
        return Success(entity: dtos);
    }
}
