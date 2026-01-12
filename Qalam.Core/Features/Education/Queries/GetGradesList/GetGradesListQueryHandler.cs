using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetGradesList;

public class GetGradesListQueryHandler : ResponseHandler,
    IRequestHandler<GetGradesListQuery, Response<PaginatedResult<Grade>>>
{
    private readonly IGradeService _gradeService;

    public GetGradesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<PaginatedResult<Grade>>> Handle(
        GetGradesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetPaginatedGradesAsync(
            request.PageNumber,
            request.PageSize,
            request.LevelId,
            request.CurriculumId,
            request.Search);

        return Success(entity: result);
    }
}
