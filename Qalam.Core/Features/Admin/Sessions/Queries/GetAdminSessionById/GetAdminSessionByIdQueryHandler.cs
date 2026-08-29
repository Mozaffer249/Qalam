using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Queries.GetAdminSessionById;

public class GetAdminSessionByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminSessionByIdQuery, Response<AdminSessionDetailDto>>
{
    private readonly IAdminSessionReadRepository _sessions;

    public GetAdminSessionByIdQueryHandler(
        IAdminSessionReadRepository sessions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _sessions = sessions;
    }

    public async Task<Response<AdminSessionDetailDto>> Handle(
        GetAdminSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _sessions.GetDetailAsync(request.Id, cancellationToken);
        if (detail == null)
            return NotFound<AdminSessionDetailDto>("Session not found.");

        return Success(entity: detail);
    }
}
