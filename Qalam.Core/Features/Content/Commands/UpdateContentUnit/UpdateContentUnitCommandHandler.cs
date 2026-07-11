using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.UpdateContentUnit;

public class UpdateContentUnitCommandHandler : ResponseHandler,
    IRequestHandler<UpdateContentUnitCommand, Response<ContentUnit>>
{
    private readonly IContentManagementService _contentService;

    public UpdateContentUnitCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<ContentUnit>> Handle(
        UpdateContentUnitCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var contentUnit = new ContentUnit
            {
                Id = request.Id,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                SubjectId = request.SubjectId,
                TermId = request.TermId,
                OrderIndex = request.OrderIndex,
                UnitTypeCode = request.UnitTypeCode,
                QuranSurahId = request.QuranSurahId > 0 ? request.QuranSurahId : null,
                QuranPartId = request.QuranPartId > 0 ? request.QuranPartId : null,
                IsActive = request.IsActive
            };

            var result = await _contentService.UpdateContentUnitAsync(contentUnit);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<ContentUnit>(ex.Message);
        }
    }
}
