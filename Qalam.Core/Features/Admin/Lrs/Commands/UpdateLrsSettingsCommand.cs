using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class UpdateLrsSettingsCommand : IRequest<Response<AdminLrsSettingsDto>>
{
    public AdminLrsSettingsDto Settings { get; set; } = default!;
}
