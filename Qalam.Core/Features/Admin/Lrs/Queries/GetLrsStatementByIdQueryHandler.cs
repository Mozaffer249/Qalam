using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class GetLrsStatementByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetLrsStatementByIdQuery, Response<AdminLrsStatementDetailDto>>
{
    private readonly ILrsStatementLogRepository _repo;

    public GetLrsStatementByIdQueryHandler(
        ILrsStatementLogRepository repo,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<AdminLrsStatementDetailDto>> Handle(
        GetLrsStatementByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (dto == null)
            return NotFound<AdminLrsStatementDetailDto>("LRS statement not found.");

        return Success(entity: dto);
    }
}
