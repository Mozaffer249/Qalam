using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.ReleaseAdminSessionEarning;

public class ReleaseAdminSessionEarningCommandHandler : ResponseHandler,
    IRequestHandler<ReleaseAdminSessionEarningCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public ReleaseAdminSessionEarningCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        ReleaseAdminSessionEarningCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.ReleaseEarningAsync(request.ScheduleId, request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Earning released.");
    }
}
