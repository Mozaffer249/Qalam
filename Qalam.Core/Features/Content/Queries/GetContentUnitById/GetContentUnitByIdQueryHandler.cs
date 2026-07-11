using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Queries.GetContentUnitById;

public class GetContentUnitByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetContentUnitByIdQuery, Response<ContentUnit>>
{
    private readonly IContentManagementService _contentService;

    public GetContentUnitByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<ContentUnit>> Handle(
        GetContentUnitByIdQuery request,
        CancellationToken cancellationToken)
    {
        var contentUnit = await _contentService.GetContentUnitByIdAsync(request.Id);

        if (contentUnit == null)
            return NotFound<ContentUnit>("Content unit not found");

        return Success(entity: contentUnit);
    }
}
