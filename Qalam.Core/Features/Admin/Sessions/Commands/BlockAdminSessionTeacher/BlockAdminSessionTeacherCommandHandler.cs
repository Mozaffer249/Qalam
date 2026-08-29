using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.BlockAdminSessionTeacher;

public class BlockAdminSessionTeacherCommandHandler : ResponseHandler,
    IRequestHandler<BlockAdminSessionTeacherCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public BlockAdminSessionTeacherCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        BlockAdminSessionTeacherCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.BlockTeacherAsync(request.ScheduleId, request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Teacher block toggled.");
    }
}
