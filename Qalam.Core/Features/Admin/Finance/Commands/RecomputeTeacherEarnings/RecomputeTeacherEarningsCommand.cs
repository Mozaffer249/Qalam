using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Commands.RecomputeTeacherEarnings;

public class RecomputeTeacherEarningsCommand : IRequest<Response<AdminTeacherEarningsRecomputeResultDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public bool DryRun { get; set; } = true;
}

public class RecomputeTeacherEarningsCommandHandler : ResponseHandler,
    IRequestHandler<RecomputeTeacherEarningsCommand, Response<AdminTeacherEarningsRecomputeResultDto>>
{
    private readonly ITeacherEarningRecomputeService _recomputeService;

    public RecomputeTeacherEarningsCommandHandler(
        ITeacherEarningRecomputeService recomputeService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _recomputeService = recomputeService;
    }

    public async Task<Response<AdminTeacherEarningsRecomputeResultDto>> Handle(
        RecomputeTeacherEarningsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _recomputeService.RecomputeAsync(
            request.DryRun,
            request.UserId > 0 ? request.UserId : null,
            cancellationToken);
        return Success(entity: result);
    }
}
