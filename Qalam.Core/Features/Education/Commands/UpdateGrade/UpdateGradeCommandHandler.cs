using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpdateGrade;

public class UpdateGradeCommandHandler : ResponseHandler,
    IRequestHandler<UpdateGradeCommand, Response<Grade>>
{
    private readonly IGradeService _gradeService;

    public UpdateGradeCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<Grade>> Handle(
        UpdateGradeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var grade = new Grade
            {
                Id = request.Id,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                LevelId = request.LevelId,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive
            };

            var result = await _gradeService.UpdateGradeAsync(grade);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Grade>(ex.Message);
        }
    }
}
