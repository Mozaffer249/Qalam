using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.CreateGrade;

public class CreateGradeCommandHandler : ResponseHandler,
    IRequestHandler<CreateGradeCommand, Response<Grade>>
{
    private readonly IGradeService _gradeService;

    public CreateGradeCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<Grade>> Handle(
        CreateGradeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var grade = new Grade
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                LevelId = request.LevelId,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive
            };

            var result = await _gradeService.CreateGradeAsync(grade);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Grade>(ex.Message);
        }
    }
}
