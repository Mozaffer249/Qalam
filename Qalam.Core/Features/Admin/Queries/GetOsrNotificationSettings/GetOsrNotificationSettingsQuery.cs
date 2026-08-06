using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Queries.GetOsrNotificationSettings;

public class GetOsrNotificationSettingsQuery : IRequest<Response<OsrNotificationSettingsDto>>
{
}
