using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class UpdateLrsSettingsCommandHandler : ResponseHandler,
    IRequestHandler<UpdateLrsSettingsCommand, Response<AdminLrsSettingsDto>>
{
    private readonly ISystemSettingRepository _settings;

    public UpdateLrsSettingsCommandHandler(
        ISystemSettingRepository settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _settings = settings;
    }

    public async Task<Response<AdminLrsSettingsDto>> Handle(
        UpdateLrsSettingsCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Settings;

        if (string.IsNullOrWhiteSpace(dto.Endpoint))
            return BadRequest<AdminLrsSettingsDto>("Endpoint is required.");
        if (string.IsNullOrWhiteSpace(dto.Key))
            return BadRequest<AdminLrsSettingsDto>("Key is required.");
        if (string.IsNullOrWhiteSpace(dto.Secret))
            return BadRequest<AdminLrsSettingsDto>("Secret is required.");

        // If secret is masked, preserve the existing one
        var existing = await _settings.GetByKeyAsync("LrsCredentials", cancellationToken);
        if (dto.Secret == "••••••••" && existing != null)
        {
            var prev = JsonSerializer.Deserialize<AdminLrsSettingsDto>(existing.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (prev != null)
                dto.Secret = prev.Secret;
        }

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await _settings.UpsertAsync(new SystemSetting
        {
            Key = "LrsCredentials",
            Value = json,
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "NELC LRS xAPI credentials and configuration",
            DescriptionAr = "بيانات اعتماد NELC LRS وإعداداتها",
        }, cancellationToken);

        return Success(entity: dto);
    }
}
