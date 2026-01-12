using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Quran.Queries.GetQuranSurahsList;

public class GetQuranSurahsListQueryHandler : ResponseHandler,
    IRequestHandler<GetQuranSurahsListQuery, Response<PaginatedResult<QuranSurah>>>
{
    private readonly IQuranService _quranService;

    public GetQuranSurahsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IQuranService quranService) : base(localizer)
    {
        _quranService = quranService;
    }

    public async Task<Response<PaginatedResult<QuranSurah>>> Handle(
        GetQuranSurahsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _quranService.GetPaginatedSurahsAsync(
            request.PageNumber,
            request.PageSize,
            request.PartNumber,
            request.Search);

        return Success(entity: result);
    }
}
