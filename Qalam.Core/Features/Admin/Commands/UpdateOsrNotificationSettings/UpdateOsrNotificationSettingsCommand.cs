using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Commands.UpdateOsrNotificationSettings;

public class UpdateOsrNotificationSettingsCommand : IRequest<Response<OsrNotificationSettingsDto>>
{
    public OsrNotificationSettingsDto Settings { get; set; } = new();
}
