using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.CancelAdminSession;

public class CancelAdminSessionCommandHandler : ResponseHandler,
    IRequestHandler<CancelAdminSessionCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public CancelAdminSessionCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        CancelAdminSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.CancelAsync(request.ScheduleId, request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Session cancelled.");
    }
}
