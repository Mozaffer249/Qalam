using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherAccessSettings;

public class GetTeacherAccessSettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAccessSettingsQuery, Response<TeacherAccessSettingsDto>>
{
    private readonly ITeacherAccessSettingsProvider _provider;

    public GetTeacherAccessSettingsQueryHandler(
        ITeacherAccessSettingsProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<TeacherAccessSettingsDto>> Handle(
        GetTeacherAccessSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _provider.GetSettingsAsync(cancellationToken);
        return Success(entity: settings);
    }
}
