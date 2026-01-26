using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Commands.CreateCurriculum;

public class CreateCurriculumCommandHandler : ResponseHandler,
    IRequestHandler<CreateCurriculumCommand, Response<Data.Entity.Education.Curriculum>>
{
    private readonly ICurriculumService _curriculumService;

    public CreateCurriculumCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<Data.Entity.Education.Curriculum>> Handle(
        CreateCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var curriculum = new Data.Entity.Education.Curriculum
            {
                DomainId = request.DomainId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                Country = request.Country,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                IsActive = request.IsActive
            };

            var result = await _curriculumService.CreateCurriculumAsync(curriculum);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Data.Entity.Education.Curriculum>(ex.Message);
        }
    }
}
