using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Live;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.GetMySessionLiveToken;

public class GetMySessionLiveTokenCommandHandler : ResponseHandler,
    IRequestHandler<GetMySessionLiveTokenCommand, Response<LiveSessionAccessDto>>
{
    private readonly ILiveSessionAccessService _liveSessionAccess;

    public GetMySessionLiveTokenCommandHandler(
        ILiveSessionAccessService liveSessionAccess,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _liveSessionAccess = liveSessionAccess;
    }

    public async Task<Response<LiveSessionAccessDto>> Handle(
        GetMySessionLiveTokenCommand request,
        CancellationToken cancellationToken)
    {
        var (ok, message, forbidden, notFound, unavailable, access) =
            await _liveSessionAccess.GetTeacherAccessAsync(request.UserId, request.Id, cancellationToken);

        if (forbidden) return Forbidden<LiveSessionAccessDto>(message);
        if (notFound) return NotFound<LiveSessionAccessDto>(message);
        if (unavailable) return ServiceUnavailable<LiveSessionAccessDto>(message);
        if (!ok || access == null) return BadRequest<LiveSessionAccessDto>(message);
        return Success(entity: access);
    }
}
