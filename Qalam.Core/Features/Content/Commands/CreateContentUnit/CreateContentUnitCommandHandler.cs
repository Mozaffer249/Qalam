using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.CreateContentUnit;

public class CreateContentUnitCommandHandler : ResponseHandler,
    IRequestHandler<CreateContentUnitCommand, Response<ContentUnit>>
{
    private readonly IContentManagementService _contentService;

    public CreateContentUnitCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<ContentUnit>> Handle(
        CreateContentUnitCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var contentUnit = new ContentUnit
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                SubjectId = request.SubjectId,
                TermId = request.TermId,
                OrderIndex = request.OrderIndex,
                UnitTypeCode = request.UnitTypeCode,
                // Convert 0 to null for optional foreign keys
                QuranSurahId = request.QuranSurahId > 0 ? request.QuranSurahId : null,
                QuranPartId = request.QuranPartId > 0 ? request.QuranPartId : null
            };

            var result = await _contentService.CreateContentUnitAsync(contentUnit);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<ContentUnit>(ex.Message);
        }
    }
}
