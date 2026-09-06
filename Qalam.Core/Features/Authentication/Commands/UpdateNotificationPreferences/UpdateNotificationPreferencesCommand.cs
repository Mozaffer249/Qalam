using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Account;

namespace Qalam.Core.Features.Authentication.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommand
    : IRequest<Response<NotificationPreferencesDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public bool PushEnabled { get; set; } = true;
    public bool EmailDigestEnabled { get; set; } = true;
    public bool SmsAlertsEnabled { get; set; }
}
