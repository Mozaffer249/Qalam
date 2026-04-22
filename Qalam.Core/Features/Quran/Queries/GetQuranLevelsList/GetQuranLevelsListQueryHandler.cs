using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Quran;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Quran.Queries.GetQuranLevelsList;

public class GetQuranLevelsListQueryHandler : ResponseHandler,
    IRequestHandler<GetQuranLevelsListQuery, Response<List<QuranLevel>>>
{
    private readonly IQuranService _quranService;

    public GetQuranLevelsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IQuranService quranService) : base(localizer)
    {
        _quranService = quranService;
    }

    public async Task<Response<List<QuranLevel>>> Handle(
        GetQuranLevelsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _quranService.GetPaginatedQuranLevelsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
