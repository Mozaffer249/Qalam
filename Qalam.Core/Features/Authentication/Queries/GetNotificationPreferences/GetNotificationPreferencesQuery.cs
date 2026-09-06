using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Account;

namespace Qalam.Core.Features.Authentication.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQuery
    : IRequest<Response<NotificationPreferencesDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
