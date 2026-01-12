using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Quran.Queries.GetQuranLevelsList;

public class GetQuranLevelsListQueryHandler : ResponseHandler,
    IRequestHandler<GetQuranLevelsListQuery, Response<PaginatedResult<QuranLevel>>>
{
    private readonly IQuranService _quranService;

    public GetQuranLevelsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IQuranService quranService) : base(localizer)
    {
        _quranService = quranService;
    }

    public async Task<Response<PaginatedResult<QuranLevel>>> Handle(
        GetQuranLevelsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _quranService.GetPaginatedQuranLevelsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(entity: result);
    }
}
