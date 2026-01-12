using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.CreateEducationLevel;

public class CreateEducationLevelCommandHandler : ResponseHandler,
    IRequestHandler<CreateEducationLevelCommand, Response<EducationLevel>>
{
    private readonly IGradeService _gradeService;

    public CreateEducationLevelCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<EducationLevel>> Handle(
        CreateEducationLevelCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var level = new EducationLevel
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                DomainId = request.DomainId,
                CurriculumId = request.CurriculumId,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive
            };

            var result = await _gradeService.CreateLevelAsync(level);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<EducationLevel>(ex.Message);
        }
    }
}
