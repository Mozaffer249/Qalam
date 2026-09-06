using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Account;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandHandler : ResponseHandler,
    IRequestHandler<UpdateNotificationPreferencesCommand, Response<NotificationPreferencesDto>>
{
    private readonly INotificationPreferenceService _preferences;

    public UpdateNotificationPreferencesCommandHandler(
        INotificationPreferenceService preferences,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _preferences = preferences;
    }

    public async Task<Response<NotificationPreferencesDto>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var dto = await _preferences.UpdateAsync(
            request.UserId,
            new NotificationPreferencesDto
            {
                PushEnabled = request.PushEnabled,
                EmailDigestEnabled = request.EmailDigestEnabled,
                SmsAlertsEnabled = request.SmsAlertsEnabled
            },
            cancellationToken);
        return Success(entity: dto);
    }
}
