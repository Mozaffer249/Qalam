using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Commands.UpdateCurriculum;

public class UpdateCurriculumCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCurriculumCommand, Response<Data.Entity.Education.Curriculum>>
{
    private readonly ICurriculumService _curriculumService;

    public UpdateCurriculumCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<Data.Entity.Education.Curriculum>> Handle(
        UpdateCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var curriculum = new Data.Entity.Education.Curriculum
            {
                Id = request.Id,
                DomainId = request.DomainId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                Country = request.Country,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                IsActive = request.IsActive
            };

            var result = await _curriculumService.UpdateCurriculumAsync(curriculum);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Data.Entity.Education.Curriculum>(ex.Message);
        }
    }
}
