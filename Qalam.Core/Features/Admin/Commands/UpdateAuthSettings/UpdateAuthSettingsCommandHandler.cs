using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Auth;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.UpdateAuthSettings;

public class UpdateAuthSettingsCommandHandler : ResponseHandler,
    IRequestHandler<UpdateAuthSettingsCommand, Response<AuthSettingsDto>>
{
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IConfiguration _configuration;

    public UpdateAuthSettingsCommandHandler(
        IAuthSettingsProvider authSettingsProvider,
        IConfiguration configuration,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _authSettingsProvider = authSettingsProvider;
        _configuration = configuration;
    }

    public async Task<Response<AuthSettingsDto>> Handle(
        UpdateAuthSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = request.Settings;

        if (UsesSms(settings.Teacher) || UsesSms(settings.Student))
        {
            var smsEnabled = _configuration.GetValue<bool>("SmsSettings:Enabled");
            if (!smsEnabled)
            {
                return BadRequest<AuthSettingsDto>(
                    "SMS delivery is not configured. Enable SmsSettings:Enabled or use Email delivery.");
            }
        }

        var saved = await _authSettingsProvider.SaveSettingsAsync(settings, cancellationToken);

        return Success("Auth settings updated successfully", entity: saved);
    }

    private static bool UsesSms(PersonaAuthSettingsDto persona) =>
        string.Equals(persona.OtpDelivery, "Sms", StringComparison.OrdinalIgnoreCase);
}
