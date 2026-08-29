using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.WarnAdminSessionTeacher;

public class WarnAdminSessionTeacherCommandHandler : ResponseHandler,
    IRequestHandler<WarnAdminSessionTeacherCommand, Response<string>>
{
    private readonly IAdminSessionActionService _actions;

    public WarnAdminSessionTeacherCommandHandler(
        IAdminSessionActionService actions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _actions = actions;
    }

    public async Task<Response<string>> Handle(
        WarnAdminSessionTeacherCommand request,
        CancellationToken cancellationToken)
    {
        await _actions.WarnTeacherAsync(
            request.ScheduleId,
            request.UserId,
            request.Notes,
            cancellationToken);

        return Success(entity: "Teacher warned.");
    }
}
