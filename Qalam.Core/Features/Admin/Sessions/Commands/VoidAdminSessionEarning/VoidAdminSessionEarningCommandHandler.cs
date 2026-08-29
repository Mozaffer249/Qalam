using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.VoidAdminSessionEarning;

public class VoidAdminSessionEarningCommandHandler : ResponseHandler,
    IRequestHandler<VoidAdminSessionEarningCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public VoidAdminSessionEarningCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        VoidAdminSessionEarningCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.VoidEarningAsync(request.ScheduleId, request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Earning voided.");
    }
}
