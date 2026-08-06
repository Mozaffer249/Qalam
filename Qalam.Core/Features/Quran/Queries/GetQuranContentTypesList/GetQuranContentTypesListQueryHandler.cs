using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Quran;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Quran.Queries.GetQuranContentTypesList;

public class GetQuranContentTypesListQueryHandler : ResponseHandler,
    IRequestHandler<GetQuranContentTypesListQuery, Response<List<QuranContentType>>>
{
    private readonly IQuranService _quranService;

    public GetQuranContentTypesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IQuranService quranService) : base(localizer)
    {
        _quranService = quranService;
    }

    public async Task<Response<List<QuranContentType>>> Handle(
        GetQuranContentTypesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _quranService.GetPaginatedContentTypesAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
