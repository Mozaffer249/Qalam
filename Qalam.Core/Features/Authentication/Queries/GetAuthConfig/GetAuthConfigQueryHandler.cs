using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Auth;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Queries.GetAuthConfig;

public class GetAuthConfigQueryHandler : ResponseHandler,
    IRequestHandler<GetAuthConfigQuery, Response<AuthConfigResponseDto>>
{
    private readonly IAuthSettingsProvider _authSettingsProvider;

    public GetAuthConfigQueryHandler(
        IAuthSettingsProvider authSettingsProvider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _authSettingsProvider = authSettingsProvider;
    }

    public async Task<Response<AuthConfigResponseDto>> Handle(
        GetAuthConfigQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        var config = _authSettingsProvider.ToPublicConfig(settings);
        return Success(entity: config);
    }
}
