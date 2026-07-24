using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.JoinMySession;

public class JoinMySessionCommandHandler : ResponseHandler,
    IRequestHandler<JoinMySessionCommand, Response<string>>
{
    private readonly ISessionPresenceService _presenceService;

    public JoinMySessionCommandHandler(
        ISessionPresenceService presenceService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _presenceService = presenceService;
    }

    public async Task<Response<string>> Handle(JoinMySessionCommand request, CancellationToken cancellationToken)
    {
        var (ok, message, forbidden, notFound) = await _presenceService.JoinAsTeacherAsync(
            request.UserId, request.Id, cancellationToken);

        if (forbidden) return Forbidden<string>(message);
        if (notFound) return NotFound<string>(message);
        if (!ok) return BadRequest<string>(message);
        return Success(entity: message);
    }
}
