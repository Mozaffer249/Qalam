using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.IssueAdminSessionRefund;

public class IssueAdminSessionRefundCommandHandler : ResponseHandler,
    IRequestHandler<IssueAdminSessionRefundCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public IssueAdminSessionRefundCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        IssueAdminSessionRefundCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.IssueRefundAsync(
                request.ScheduleId,
                request.UserId,
                request.Body,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Refund issued.");
    }
}
