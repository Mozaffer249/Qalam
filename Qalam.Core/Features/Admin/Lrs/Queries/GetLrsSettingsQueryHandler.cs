using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class GetLrsSettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetLrsSettingsQuery, Response<AdminLrsSettingsDto>>
{
    private readonly ISystemSettingRepository _settings;

    public GetLrsSettingsQueryHandler(
        ISystemSettingRepository settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _settings = settings;
    }

    public async Task<Response<AdminLrsSettingsDto>> Handle(
        GetLrsSettingsQuery request, CancellationToken cancellationToken)
    {
        var row = await _settings.GetByKeyAsync("LrsCredentials", cancellationToken);

        if (row != null && !string.IsNullOrEmpty(row.Value))
        {
            var dto = JsonSerializer.Deserialize<AdminLrsSettingsDto>(row.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto != null)
                return Success(entity: dto);
        }

        return Success(entity: new AdminLrsSettingsDto
        {
            Enabled = false,
            Endpoint = "",
            Key = "",
            Secret = "",
            MaxRetries = 3,
            PlatformIri = "https://qalam.net.sa",
            XApiVersion = "1.0.3",
            PlatformKey = "",
            PlatformNameAr = "قلم",
            PlatformNameEn = "Qalam",
        });
    }
}
