using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetGradesList;

public class GetGradesListQueryHandler : ResponseHandler,
    IRequestHandler<GetGradesListQuery, Response<List<GradeDto>>>
{
    private readonly IGradeService _gradeService;

    public GetGradesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<List<GradeDto>>> Handle(
        GetGradesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetPaginatedGradesAsync(
            request.PageNumber,
            request.PageSize,
            request.LevelId,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
