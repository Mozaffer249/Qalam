using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class GetLrsDashboardQueryHandler : ResponseHandler,
    IRequestHandler<GetLrsDashboardQuery, Response<AdminLrsDashboardDto>>
{
    private readonly ILrsStatementLogRepository _repo;

    public GetLrsDashboardQueryHandler(
        ILrsStatementLogRepository repo,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<AdminLrsDashboardDto>> Handle(
        GetLrsDashboardQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await _repo.GetDashboardAsync(cancellationToken);
        return Success(entity: dashboard);
    }
}
