using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Queries.GetCurriculumsList;

public class GetCurriculumsListQueryHandler : ResponseHandler,
    IRequestHandler<GetCurriculumsListQuery, Response<List<CurriculumDto>>>
{
    private readonly ICurriculumService _curriculumService;

    public GetCurriculumsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<List<CurriculumDto>>> Handle(
        GetCurriculumsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _curriculumService.GetPaginatedCurriculumsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.DomainId);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
