using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Core.Features.Admin.Commands.UpdateAuthSettings;

public class UpdateAuthSettingsCommand : IRequest<Response<AuthSettingsDto>>
{
    public AuthSettingsDto Settings { get; set; } = new();
}
