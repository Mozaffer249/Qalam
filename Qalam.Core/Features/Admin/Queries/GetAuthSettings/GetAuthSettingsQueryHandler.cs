using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Auth;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetAuthSettings;

public class GetAuthSettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetAuthSettingsQuery, Response<AuthSettingsDto>>
{
    private readonly IAuthSettingsProvider _authSettingsProvider;

    public GetAuthSettingsQueryHandler(
        IAuthSettingsProvider authSettingsProvider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _authSettingsProvider = authSettingsProvider;
    }

    public async Task<Response<AuthSettingsDto>> Handle(
        GetAuthSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        return Success(entity: settings);
    }
}
