using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetLevelsList;

public class GetLevelsListQueryHandler : ResponseHandler,
    IRequestHandler<GetLevelsListQuery, Response<PaginatedResult<EducationLevelDto>>>
{
    private readonly IGradeService _gradeService;

    public GetLevelsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<PaginatedResult<EducationLevelDto>>> Handle(
        GetLevelsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetPaginatedLevelsAsync(
            request.PageNumber,
            request.PageSize,
            request.CurriculumId,
            request.Search);

        return Success(entity: result);
    }
}
