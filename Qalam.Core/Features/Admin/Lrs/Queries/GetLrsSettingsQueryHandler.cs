using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class GetLrsSettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetLrsSettingsQuery, Response<AdminLrsSettingsDto>>
{
    private readonly ISystemSettingRepository _settings;
    private readonly LrsSettings _envSettings;

    public GetLrsSettingsQueryHandler(
        ISystemSettingRepository settings,
        IOptions<LrsSettings> envSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _settings = settings;
        _envSettings = envSettings.Value;
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

        // Fall back to env-based settings (mask the secret)
        return Success(entity: new AdminLrsSettingsDto
        {
            Enabled = _envSettings.Enabled,
            Endpoint = _envSettings.Endpoint,
            Key = _envSettings.Key,
            Secret = string.IsNullOrEmpty(_envSettings.Secret) ? "" : "••••••••",
            MaxRetries = _envSettings.MaxRetries,
            PlatformIri = _envSettings.PlatformIri,
            XApiVersion = _envSettings.XApiVersion,
            PlatformKey = _envSettings.PlatformKey,
            PlatformNameAr = _envSettings.PlatformNameAr,
            PlatformNameEn = _envSettings.PlatformNameEn,
        });
    }
}
