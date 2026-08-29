using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.HoldAdminSessionEarning;

public class HoldAdminSessionEarningCommandHandler : ResponseHandler,
    IRequestHandler<HoldAdminSessionEarningCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public HoldAdminSessionEarningCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        HoldAdminSessionEarningCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.HoldEarningAsync(request.ScheduleId, request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Earning held.");
    }
}
