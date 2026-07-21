using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Queries.ListNationalities;

public class ListNationalitiesQueryHandler : ResponseHandler,
    IRequestHandler<ListNationalitiesQuery, Response<List<NationalityAdminDto>>>
{
    private readonly INationalityProvider _provider;

    public ListNationalitiesQueryHandler(
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<List<NationalityAdminDto>>> Handle(
        ListNationalitiesQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _provider.GetAllAsync(cancellationToken);
        var dtos = entities.Select(_provider.ToAdminDto).ToList();
        return Success(entity: dtos);
    }
}
