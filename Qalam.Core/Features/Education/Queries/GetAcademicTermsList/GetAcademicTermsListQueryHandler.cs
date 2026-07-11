using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetAcademicTermsList;

public class GetAcademicTermsListQueryHandler : ResponseHandler,
    IRequestHandler<GetAcademicTermsListQuery, Response<List<AcademicTermDto>>>
{
    private readonly IGradeService _gradeService;

    public GetAcademicTermsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<List<AcademicTermDto>>> Handle(
        GetAcademicTermsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetPaginatedTermsAsync(
            request.PageNumber,
            request.PageSize,
            request.CurriculumId);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
