using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Core.Features.Admin.Queries.GetAuthSettings;

public class GetAuthSettingsQuery : IRequest<Response<AuthSettingsDto>>
{
}
