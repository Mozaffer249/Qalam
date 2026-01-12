using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Quran;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Quran.Queries.GetQuranPartsList;

public class GetQuranPartsListQueryHandler : ResponseHandler,
    IRequestHandler<GetQuranPartsListQuery, Response<List<QuranPart>>>
{
    private readonly IQuranService _quranService;

    public GetQuranPartsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IQuranService quranService) : base(localizer)
    {
        _quranService = quranService;
    }

    public async Task<Response<List<QuranPart>>> Handle(
        GetQuranPartsListQuery request,
        CancellationToken cancellationToken)
    {
        var parts = await _quranService.GetQuranPartsQueryable()
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);

        return Success(entity: parts);
    }
}
