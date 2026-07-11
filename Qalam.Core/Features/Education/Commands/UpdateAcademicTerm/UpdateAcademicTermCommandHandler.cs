using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpdateAcademicTerm;

public class UpdateAcademicTermCommandHandler : ResponseHandler,
    IRequestHandler<UpdateAcademicTermCommand, Response<AcademicTerm>>
{
    private readonly IGradeService _gradeService;

    public UpdateAcademicTermCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<AcademicTerm>> Handle(
        UpdateAcademicTermCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var term = new AcademicTerm
            {
                Id = request.Id,
                CurriculumId = request.CurriculumId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                OrderIndex = request.OrderIndex,
                IsMandatory = request.IsMandatory,
                IsActive = request.IsActive
            };

            var result = await _gradeService.UpdateTermAsync(term);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AcademicTerm>(ex.Message);
        }
    }
}
